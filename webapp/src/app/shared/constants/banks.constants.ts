export interface Bank {
  name: string;
  fullname: string;
  nameEN: string;
  symbol: string;
  icon: string;
  color: string;
}

export interface BankList {
  [key: string]: Bank;
}

export const bankLists: BankList = {
  CASH: {
    name: "เงินสด",
    fullname: "เงินสด",
    nameEN: "Cash",
    symbol: "CASH",
    icon: "banks/cash.jpg",
    color: "#27AE60"
  },
  KBANK: {
    name: "กสิกรไทย",
    fullname: "ธนาคารกสิกรไทย",
    nameEN: "Kasikorn Bank",
    symbol: "KBANK",
    icon: "banks/KBANK.png",
    color: "#1DA858"
  },
  SCB: {
    name: "ไทยพาณิชย์",
    fullname: "ธนาคารไทยพาณิชย์",
    nameEN: "The Siam Commercial Bank",
    symbol: "SCB",
    icon: "banks/SCB.png",
    color: "#543186"
  },
  KTB: {
    name: "กรุงไทย",
    fullname: "ธนาคารกรุงไทย",
    nameEN: "Krungthai Bank",
    symbol: "KTB",
    icon: "banks/KTB.png",
    color: "#1DA8E6"
  },
  BBL: {
    name: "กรุงเทพ",
    fullname: "ธนาคารกรุงเทพ",
    nameEN: "Bangkok Bank",
    symbol: "BBL",
    icon: "banks/BBL.png",
    color: "#29449D"
  },
  BAY: {
    name: "กรุงศรีอยุธยา",
    fullname: "ธนาคารกรุงศรีอยุธยา",
    nameEN: "Krungsri Bank",
    symbol: "BAY",
    icon: "banks/BAY.png",
    color: "#FFD51C"
  },
  TTB: {
    name: "ทีเอ็มบีธนชาต",
    fullname: "ธนาคารทีเอ็มบีธนชาต",
    nameEN: "TMBThanachart Bank",
    symbol: "TTB",
    icon: "banks/TTB.png",
    color: "#0C55F2"
  },
  UOB: {
    name: "ยูโอบี",
    fullname: "ธนาคารยูโอบี",
    nameEN: "United Overseas Bank",
    symbol: "UOB",
    icon: "banks/UOB.png",
    color: "#E41A26"
  },
  KKP: {
    name: "เกียรตินาคิน",
    fullname: "ธนาคารเกียรตินาคินภัทร",
    nameEN: "Kiatnakin Phatra Bank",
    symbol: "KKP",
    icon: "banks/KKP.png",
    color: "#5A547C"
  },
  GSB: {
    name: "ออมสิน",
    fullname: "ธนาคารออมสิน",
    nameEN: "Government Savings Bank",
    symbol: "GSB",
    icon: "banks/GSB.png",
    color: "#ED1891"
  },
  BAAC: {
    name: "ธ.ก.ส.",
    fullname: "ธนาคารเพื่อการเกษตรและสหกรณ์การเกษตร",
    nameEN: "Bank for Agriculture and Agricultural Cooperatives",
    symbol: "BAAC",
    icon: "banks/BAAC.png",
    color: "#CCA41C"
  },
  CIMB: {
    name: "ซีไอเอ็มบี",
    fullname: "ธนาคารซีไอเอ็มบี",
    nameEN: "CIMB Thai Bank",
    symbol: "CIMB",
    icon: "banks/CIMB.png",
    color: "#BD1325"
  },
  CITI: {
    name: "ซิตี้แบงก์",
    fullname: "ธนาคารซิตี้แบงก์",
    nameEN: "citibank",
    symbol: "CITI",
    icon: "banks/CITI.png",
    color: "#0F3D89"
  },
  GHB: {
    name: "ธ.อ.ส.",
    fullname: "ธนาคารอาคารสงเคราะห์",
    nameEN: "GH Bank",
    symbol: "GHB",
    icon: "banks/GHB.png",
    color: "#FF8614"
  },
  HSBC: {
    name: "เอชเอสบีซี",
    fullname: "ธนาคารเอชเอสบีซี",
    nameEN: "HSBC Bank",
    symbol: "HSBC",
    icon: "banks/HSBC.png",
    color: "#FF1518"
  },
  IBANK: {
    name: "อิสลามแห่งประเทศไทย",
    fullname: "ธนาคารอิสลามแห่งประเทศไทย",
    nameEN: "Islamic Bank of Thailand",
    symbol: "IBANK",
    icon: "banks/IBANK.png",
    color: "#164626"
  },
  ICBC: {
    name: "ไอซีบีซี",
    fullname: "ธนาคารไอซีบีซี",
    nameEN: "ICBC Thai Commercial Bank",
    symbol: "ICBC",
    icon: "banks/ICBC.png",
    color: "#CD1511"
  },
  LHB: {
    name: "แลนด์ แอนด์ เฮ้าส์",
    fullname: "ธนาคารแลนด์ แอนด์ เฮ้าส์",
    nameEN: "LH Bank",
    symbol: "LHB",
    icon: "banks/LHB.png",
    color: "#727375"
  },
  TCRB: {
    name: "ไทยเครดิต",
    fullname: "ธนาคารไทยเครดิต",
    nameEN: "Thai Credit Bank",
    symbol: "TCRB",
    icon: "banks/TCRB.png",
    color: "#FF7813"
  },
  TISCO: {
    name: "ทิสโก้",
    fullname: "ธนาคารทิสโก้",
    nameEN: "Tisco Bank",
    symbol: "TISCO",
    icon: "banks/TISCO.png",
    color: "#267CBC"
  },
  PromptPay: {
    name: "พร้อมเพย์",
    fullname: "พร้อมเพย์",
    nameEN: "PromptPay",
    symbol: "PromptPay",
    icon: "banks/PromptPay.png",
    color: "#0C4370"
  },
  TrueMoney: {
    name: "ทรูมันนี่",
    fullname: "ทรูมันนี่",
    nameEN: "True Money",
    symbol: "TrueMoney",
    icon: "banks/TrueMoney.png",
    color: "#EE252B"
  }
};
