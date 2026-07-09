/**
 * Main Application Coordinator for Packing Optimizer Pro
 * Bootstraps databases, panels, theme switching, sidebar tabs, and run loop in Thai.
 */

document.addEventListener('DOMContentLoaded', () => {
    // 1. Toast Notification System
    window.showToast = function(message, type = 'success') {
        const container = document.getElementById('toast-container');
        if (!container) return;

        const toast = document.createElement('div');
        toast.className = `toast ${type}`;
        
        let icon = 'ℹ️';
        if (type === 'success') icon = '✅';
        if (type === 'error') icon = '❌';
        if (type === 'warning') icon = '⚠️';

        toast.innerHTML = `<span>${icon}</span> <span>${message}</span>`;
        container.appendChild(toast);

        // Animation entry
        setTimeout(() => toast.classList.add('show'), 10);

        // Remove after 3.5s
        setTimeout(() => {
            toast.classList.remove('show');
            setTimeout(() => toast.remove(), 350);
        }, 3500);
    };

    // 2. Theme Switching Logic
    const themeToggle = document.getElementById('theme-toggle');
    const themeIcon = document.getElementById('theme-icon');
    const themeText = document.getElementById('theme-text');
    const htmlTag = document.documentElement;

    function applyTheme(theme) {
        htmlTag.setAttribute('data-theme', theme);
        localStorage.setItem('po_theme', theme);
        if (theme === 'dark') {
            themeIcon.textContent = '☀️';
            themeText.textContent = 'โหมดสว่าง';
        } else {
            themeIcon.textContent = '🌙';
            themeText.textContent = 'โหมดมืด';
        }
    }

    // Load initial theme
    const savedTheme = localStorage.getItem('po_theme') || (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');
    applyTheme(savedTheme);

    themeToggle.addEventListener('click', () => {
        const currentTheme = htmlTag.getAttribute('data-theme');
        const nextTheme = currentTheme === 'dark' ? 'light' : 'dark';
        applyTheme(nextTheme);
    });

    // 3. Sidebar Tab Toggling
    const tabProducts = document.getElementById('tab-products');
    const tabBoxes = document.getElementById('tab-boxes');
    const paneProducts = document.getElementById('pane-products');
    const paneBoxes = document.getElementById('pane-boxes');

    tabProducts.addEventListener('click', () => {
        document.querySelectorAll('.tab-btn').forEach(btn => btn.classList.remove('active'));
        document.querySelectorAll('.tab-pane').forEach(p => p.classList.remove('active'));
        tabProducts.classList.add('active');
        paneProducts.classList.add('active');
    });

    tabBoxes.addEventListener('click', () => {
        document.querySelectorAll('.tab-btn').forEach(btn => btn.classList.remove('active'));
        document.querySelectorAll('.tab-pane').forEach(p => p.classList.remove('active'));
        tabBoxes.classList.add('active');
        paneBoxes.classList.add('active');
    });

    // 4. Initialize Database and UI Panels
    const productDb = new window.ProductDatabase();
    const boxDb = new window.BoxDatabase();

    const resultPanel = new window.ResultPanel('results-panel-container');
    const canvasRenderer = new window.CanvasRenderer('packing-canvas');

    const dashboard = new window.Dashboard(productDb, boxDb, (products, boxes, settings) => {
        runMockOptimization(products, boxes, settings);
    });

    const simulatorPanel = new window.SimulatorPanel(
        productDb,
        boxDb,
        (simulationResult) => {
            resultPanel.render(simulationResult);
            if (simulationResult.parcels.length > 0) {
                canvasRenderer.draw(simulationResult.parcels[0]);
            }
        }
    );

    const productPanel = new window.ProductPanel(
        productDb, 
        'product-list-container', 
        'product-modal', 
        'product-form',
        () => {
            dashboard.updateStats();
            simulatorPanel.updateSkuDropdown();
        }
    );

    const boxPanel = new window.BoxPanel(
        boxDb, 
        'box-list-container', 
        'box-modal', 
        'box-form',
        () => {
            dashboard.updateStats();
        }
    );

    // Initial Stats update
    dashboard.updateStats();

    // 5. Optimization Coordinator using central ShipmentEngine
    function runMockOptimization(products, boxes, settings) {
        const optimization = ShipmentEngine.optimizePacking(products, boxes, settings);
        
        if (!optimization.success) {
            showToast(optimization.message, 'error');
            resultPanel.render(null); // Displays failed status
            return;
        }

        const parcels = optimization.parcels;

        // Calculate detailed packaging costs
        parcels.forEach(p => {
            p.packagingDetails = MathUtil.calculatePackagingDetails(p, settings, productDb);
        });

        const totalBoxCost = parcels.reduce((acc, p) => acc + p.box.cost, 0);
        const totalTapeCost = parcels.reduce((acc, p) => acc + p.packagingDetails.tapeCost, 0);
        const totalBubbleCost = parcels.reduce((acc, p) => acc + p.packagingDetails.bubbleCost, 0);
        const totalLabelCost = parcels.reduce((acc, p) => acc + p.packagingDetails.labelCost, 0);
        const totalCost = totalBoxCost + totalTapeCost + totalBubbleCost + totalLabelCost;

        const avgVolumeUtilization = parcels.reduce((acc, p) => acc + p.volumeUtilization, 0) / parcels.length;
        const overallScore = Math.max(10, Math.min(100, 100 - (parcels.length - 1) * 10 + (avgVolumeUtilization - 70)));

        const totalCargoWeight  = products.reduce((acc, p) => acc + (p.weight * p.quantity), 0);
        const totalTareWeight   = parcels.reduce((acc, p) => acc + (p.box.tare || 0), 0);

        const results = {
            parcels: parcels,
            overallScore: overallScore,
            totalBoxCost: totalBoxCost,
            totalTapeCost: totalTapeCost,
            totalBubbleCost: totalBubbleCost,
            totalLabelCost: totalLabelCost,
            totalCost: totalCost,
            totalVolumeUtilization: avgVolumeUtilization,
            totalWeight: totalCargoWeight,
            totalTareWeight: totalTareWeight,
            totalParcelWeight: totalCargoWeight + totalTareWeight
        };

        // Render results
        resultPanel.render(results);
        if (parcels.length > 0) {
            canvasRenderer.draw(parcels[0]);
        }
    }
});
