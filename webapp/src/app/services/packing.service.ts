import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';
import * as XLSX from 'xlsx';
import QRCode from 'qrcode';

export interface ParsedItem {
  platform: 'Lazada' | 'Shopee';
  orderNo: string;
  trackingNo: string;
  shippingProvider: string;
  sku: string;
  productName: string;
  orderedQuantity: number;
  shippedQuantity: number;
  orderDate?: string;
  isMultiParcel: boolean;
  parcelSeq: number;
  totalParcelsInOrder: number;
  isSkuMatched: boolean;
  variantId?: number;
  qrCodeUrl?: string;
  fileName: string;
}

export interface ParsedParcelCard {
  platform: 'Lazada' | 'Shopee';
  orderNo: string;
  trackingNo: string;
  shippingProvider: string;
  orderDate?: string;
  isMultiParcel: boolean;
  parcelSeq: number;
  totalParcelsInOrder: number;
  allTrackingNosInOrder: string[];
  trackingQrUrl?: string;
  orderQrUrl?: string;
  items: ParsedItem[];
}

export interface FileParseResult {
  fileName: string;
  platform: 'Lazada' | 'Shopee' | 'Unknown';
  isValid: boolean;
  errorMessage?: string;
  items: ParsedItem[];
}

export interface SkuCheckResult {
  isMatched: boolean;
  variantId?: number;
  variantName?: string;
}

@Injectable({
  providedIn: 'root'
})
export class PackingService {
  private readonly apiUrl = `${environment.apiUrl}/packing`;

  constructor(private http: HttpClient) {}

  cleanSkuPrefix(sku: string): string {
    if (!sku) return '';
    return sku.replace(/^\[[A-Za-z0-9_-]+\]\s*/i, '').trim();
  }

  async parseExcelFiles(files: File[]): Promise<{ results: FileParseResult[]; parcelCards: ParsedParcelCard[]; summary: { totalFiles: number; totalOrders: number; totalParcels: number; totalItems: number; matchedSkus: number; unmatchedSkus: number } }> {
    const results: FileParseResult[] = [];
    const allRawItems: ParsedItem[] = [];

    for (const file of files) {
      try {
        const data = await file.arrayBuffer();
        const workbook = XLSX.read(data, { type: 'array' });
        const firstSheetName = workbook.SheetNames[0];
        const sheet = workbook.Sheets[firstSheetName];
        const rows: any[] = XLSX.utils.sheet_to_json(sheet, { defval: '' });

        if (!rows || rows.length === 0) {
          results.push({
            fileName: file.name,
            platform: 'Unknown',
            isValid: false,
            errorMessage: 'ไฟล์ไม่มีข้อมูลแถวรายการ',
            items: []
          });
          continue;
        }

        const headers = Object.keys(rows[0]).map(h => h.trim());
        const platform = this.detectPlatform(headers);

        if (platform === 'Unknown') {
          results.push({
            fileName: file.name,
            platform: 'Unknown',
            isValid: false,
            errorMessage: `ไฟล์ '${file.name}' มีรูปแบบโครงสร้างคอลัมน์ไม่ถูกต้องตามมาตรฐานของ Lazada หรือ Shopee`,
            items: []
          });
          continue;
        }

        const items: ParsedItem[] = [];

        if (platform === 'Lazada') {
          for (const r of rows) {
            const orderNo = String(r['orderNumber'] || r['orderItemId'] || '').trim();
            const trackingNo = String(r['trackingCode'] || r['cdTrackingCode'] || '').trim();
            const sku = String(r['sellerSku'] || r['lazadaSku'] || '').trim();
            const shippingProvider = String(r['shippingProviderFM'] || r['shippingProvider'] || 'LEX TH').trim();
            const productName = String(r['itemName'] || '').trim();
            const orderDate = String(r['createTime'] || '').trim();

            if (orderNo && trackingNo && sku) {
              items.push({
                platform: 'Lazada',
                orderNo,
                trackingNo,
                shippingProvider,
                sku,
                productName,
                orderedQuantity: 1,
                shippedQuantity: 0,
                orderDate,
                isMultiParcel: false,
                parcelSeq: 1,
                totalParcelsInOrder: 1,
                isSkuMatched: false,
                fileName: file.name
              });
            }
          }
        } else if (platform === 'Shopee') {
          for (const r of rows) {
            const orderNo = String(r['หมายเลขคำสั่งซื้อ'] || '').trim();
            const trackingNo = String(r['*หมายเลขติดตามพัสดุ'] || r['หมายเลขติดตามพัสดุ'] || '').trim();
            const sku = String(r['เลขอ้างอิง SKU (SKU Reference No.)'] || r['เลขอ้างอิง Parent SKU'] || '').trim();
            const shippingProvider = String(r['ตัวเลือกการจัดส่ง'] || 'SPX Express').trim();
            const productName = String(r['ชื่อสินค้า'] || '').trim();
            const orderDate = String(r['วันที่ทำการสั่งซื้อ'] || '').trim();
            const qtyRaw = parseInt(r['จำนวน'], 10);
            const orderedQuantity = isNaN(qtyRaw) ? 1 : Math.max(1, qtyRaw);

            if (orderNo && trackingNo && sku) {
              items.push({
                platform: 'Shopee',
                orderNo,
                trackingNo,
                shippingProvider,
                sku,
                productName,
                orderedQuantity,
                shippedQuantity: 0,
                orderDate,
                isMultiParcel: false,
                parcelSeq: 1,
                totalParcelsInOrder: 1,
                isSkuMatched: false,
                fileName: file.name
              });
            }
          }
        }

        results.push({
          fileName: file.name,
          platform,
          isValid: true,
          items
        });
        allRawItems.push(...items);

      } catch (err: any) {
        results.push({
          fileName: file.name,
          platform: 'Unknown',
          isValid: false,
          errorMessage: `ไม่สามารถเปิดอ่านไฟล์ได้: ${err?.message || 'ข้อผิดพลาดโครงสร้าง'}`,
          items: []
        });
      }
    }

    // REAL-TIME DATABASE SKU CHECKING (USING CORRECT API BASE URL)
    const rawSkus = Array.from(new Set(allRawItems.map(i => i.sku)));
    if (rawSkus.length > 0) {
      try {
        const skuCheckMap = await firstValueFrom(this.checkSkus(rawSkus));
        for (const item of allRawItems) {
          const match = skuCheckMap[item.sku] || skuCheckMap[this.cleanSkuPrefix(item.sku)];
          if (match && match.isMatched) {
            item.isSkuMatched = true;
            item.variantId = match.variantId;
            if (match.variantName) {
              item.productName = match.variantName;
            }
          } else {
            item.isSkuMatched = false;
            item.variantId = undefined;
          }
        }
      } catch (e) {
        console.warn('Real-time SKU DB check failed, defaulting to unmatched/local', e);
      }
    }

    // Process & Aggregate Items per Parcel/Tracking
    const parcelCards = await this.buildParcelCards(allRawItems);

    let totalItems = 0;
    let matchedSkus = 0;
    let unmatchedSkus = 0;

    for (const card of parcelCards) {
      for (const item of card.items) {
        totalItems += item.orderedQuantity;
        if (item.isSkuMatched) matchedSkus++;
        else unmatchedSkus++;
      }
    }

    const uniqueOrders = new Set(parcelCards.map(c => c.orderNo)).size;

    return {
      results,
      parcelCards,
      summary: {
        totalFiles: files.length,
        totalOrders: uniqueOrders,
        totalParcels: parcelCards.length,
        totalItems,
        matchedSkus,
        unmatchedSkus
      }
    };
  }

  detectPlatform(headers: string[]): 'Lazada' | 'Shopee' | 'Unknown' {
    const set = new Set(headers.map(h => h.trim()));
    if (set.has('lazadaId') || set.has('sellerSku') || set.has('orderNumber')) {
      return 'Lazada';
    }
    if (set.has('*หมายเลขติดตามพัสดุ') || set.has('หมายเลขคำสั่งซื้อ') || headers.some(h => h.includes('Shopee'))) {
      return 'Shopee';
    }
    return 'Unknown';
  }

  async buildParcelCards(items: ParsedItem[]): Promise<ParsedParcelCard[]> {
    const orderTrackingMap = new Map<string, Set<string>>();
    for (const item of items) {
      if (!orderTrackingMap.has(item.orderNo)) {
        orderTrackingMap.set(item.orderNo, new Set());
      }
      orderTrackingMap.get(item.orderNo)!.add(item.trackingNo);
    }

    const parcelGroupMap = new Map<string, ParsedItem[]>();
    for (const item of items) {
      if (!parcelGroupMap.has(item.trackingNo)) {
        parcelGroupMap.set(item.trackingNo, []);
      }
      parcelGroupMap.get(item.trackingNo)!.push(item);
    }

    const cards: ParsedParcelCard[] = [];

    for (const [trackingNo, rawList] of parcelGroupMap.entries()) {
      const first = rawList[0];
      const allTrackingsInOrder = Array.from(orderTrackingMap.get(first.orderNo) || []);
      const totalParcelsInOrder = allTrackingsInOrder.length;
      const parcelSeq = allTrackingsInOrder.indexOf(trackingNo) + 1;
      const isMultiParcel = totalParcelsInOrder > 1;

      const skuMap = new Map<string, ParsedItem>();
      for (const item of rawList) {
        if (!skuMap.has(item.sku)) {
          skuMap.set(item.sku, {
            ...item,
            orderedQuantity: item.orderedQuantity,
            isMultiParcel,
            parcelSeq,
            totalParcelsInOrder
          });
        } else {
          const existing = skuMap.get(item.sku)!;
          existing.orderedQuantity += item.orderedQuantity;
        }
      }

      const cardItems = Array.from(skuMap.values());

      const trackingQrUrl = await this.generateSquareQRCode(trackingNo);
      const orderQrUrl = await this.generateSquareQRCode(first.orderNo);

      for (const it of cardItems) {
        it.qrCodeUrl = await this.generateSquareQRCode(it.sku);
      }

      cards.push({
        platform: first.platform,
        orderNo: first.orderNo,
        trackingNo,
        shippingProvider: first.shippingProvider,
        orderDate: first.orderDate,
        isMultiParcel,
        parcelSeq,
        totalParcelsInOrder,
        allTrackingNosInOrder: allTrackingsInOrder,
        trackingQrUrl,
        orderQrUrl,
        items: cardItems
      });
    }

    return cards;
  }

  async generateSquareQRCode(text: string): Promise<string> {
    try {
      return await QRCode.toDataURL(text, {
        errorCorrectionLevel: 'M',
        margin: 1,
        width: 150
      });
    } catch {
      return '';
    }
  }

  checkSkus(skus: string[]): Observable<{ [key: string]: SkuCheckResult }> {
    return this.http.post<{ [key: string]: SkuCheckResult }>(`${this.apiUrl}/check-skus`, skus);
  }

  saveDraftBatch(payload: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/draft-batch`, payload);
  }

  getBatches(pageNumber = 1, pageSize = 15): Observable<any> {
    return this.http.get(`${this.apiUrl}/batches`, { params: { pageNumber, pageSize } });
  }

  getBatchById(batchId: number): Observable<any> {
    return this.http.get(`${this.apiUrl}/batches/${batchId}`);
  }

  confirmShipment(payload: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/confirm-shipment`, payload);
  }

  getPendingBackorders(): Observable<any> {
    return this.http.get(`${this.apiUrl}/backorders`);
  }

  resolveBackorder(payload: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/resolve-backorder`, payload);
  }
}
