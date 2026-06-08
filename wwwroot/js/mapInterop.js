window.mapInterop = {
    map: null,
    heatLayer: null,
    stageLayerGroup: null,
    markers: [],

    initializeMap: function (elementId, baseLat, baseLng) {
        if (this.map) {
            this.map.remove();
        }

        // Initialize map
        this.map = L.map(elementId, {
            zoomControl: false // We will use custom zoom controls if needed, or default is fine but disabling for cleaner look
        }).setView([baseLat, baseLng], 16);

        // Add dark-themed tiles (CartoDB Dark Matter)
        L.tileLayer('https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png', {
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors &copy; <a href="https://carto.com/attributions">CARTO</a>',
            subdomains: 'abcd',
            maxZoom: 20
        }).addTo(this.map);

        this.stageLayerGroup = L.layerGroup().addTo(this.map);

        // Initialize empty heat layer
        this.heatLayer = L.heatLayer([], {
            radius: 25,
            blur: 15,
            maxZoom: 17,
            max: 1.0,
            gradient: { 0.4: 'blue', 0.6: 'cyan', 0.7: 'lime', 0.8: 'yellow', 1.0: 'red' }
        }).addTo(this.map);
    },

    drawStages: function (stages) {
        if (!this.map || !this.stageLayerGroup) return;

        this.stageLayerGroup.clearLayers();

        stages.forEach(stage => {
            L.circle([stage.latitude, stage.longitude], {
                color: '#3b82f6', // accent blue
                fillColor: '#3b82f6',
                fillOpacity: 0.1,
                weight: 2,
                radius: stage.radiusMeters
            }).bindTooltip(stage.stageName, { permanent: true, direction: 'center', className: 'stage-tooltip' }).addTo(this.stageLayerGroup);
        });
    },

    updateHeatmap: function (points) {
        if (!this.heatLayer) return;

        // Points is an array of objects: { latitude, longitude, intensity }
        const heatPoints = points.map(p => [p.latitude, p.longitude, p.intensity]);

        this.heatLayer.setLatLngs(heatPoints);
    }
};
