/**
 * vehicle-map.js
 * OpenStreetMap & Leaflet Integration for Searching Nearby Rental Vehicles
 * Powered by OpenStreetMap Tiles & Nominatim Geocoding API
 */

(function () {
    'use strict';

    // State
    let map = null;
    let userMarker = null;
    let radiusCircle = null;
    let vehicleMarkersGroup = null;
    let vehicleMarkersMap = {}; // ID -> Marker reference

    let currentLat = 21.1702; // Default: Surat Fleet Base
    let currentLon = 72.8311;
    let currentRadiusKm = 25;
    let currentCategory = 'All';

    // DOM Elements
    const mapElement = document.getElementById('vehicleMap');
    const searchInput = document.getElementById('osmSearchInput');
    const searchBtn = document.getElementById('btnOsmSearch');
    const myLocationBtn = document.getElementById('btnMyLocation');
    const radiusSelect = document.getElementById('radiusSelect');
    const categorySelect = document.getElementById('categorySelect');
    const statusText = document.getElementById('locationStatusText');
    const coordsBadge = document.getElementById('coordsBadge');
    const foundCountBadge = document.getElementById('foundCountBadge');
    const sidebarCountBadge = document.getElementById('sidebarCountBadge');
    const locationSpinner = document.getElementById('locationSpinner');
    const vehiclesListContainer = document.getElementById('vehiclesListContainer');
    const emptyStateContainer = document.getElementById('emptyStateContainer');
    const spawnDemoBtn = document.getElementById('btnSpawnDemo');
    const expandRadiusBtn = document.getElementById('btnExpandRadius');
    const sfFleetBtn = document.getElementById('btnShowSanFrancisco');

    if (!mapElement) return;

    // Helper: calculate appropriate zoom level based on search radius
    function getZoomForRadius(radiusKm) {
        if (radiusKm <= 0) return 3;
        if (radiusKm <= 5) return 14;
        if (radiusKm <= 10) return 13;
        if (radiusKm <= 25) return 12;
        if (radiusKm <= 50) return 10;
        return 9;
    }

    // Initialize Leaflet Map with OpenStreetMap Tile Layer
    function initMap() {
        map = L.map('vehicleMap', {
            center: [currentLat, currentLon],
            zoom: getZoomForRadius(currentRadiusKm),
            zoomControl: true
        });

        // Official OpenStreetMap Tile Layer
        L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
            maxZoom: 19,
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright" target="_blank">OpenStreetMap</a> contributors'
        }).addTo(map);

        // Vehicle markers group
        vehicleMarkersGroup = L.layerGroup().addTo(map);

        // Create Custom User Location Marker
        const userIcon = L.divIcon({
            className: 'user-marker-wrapper',
            html: '<div class="user-marker-pulse"></div><div class="user-marker-core"><i class="bi bi-geo-alt-fill"></i></div>',
            iconSize: [26, 26],
            iconAnchor: [0, 0]
        });

        userMarker = L.marker([currentLat, currentLon], {
            icon: userIcon,
            draggable: true,
            title: 'Your Search Location'
        }).addTo(map);

        userMarker.bindPopup('<b>Search Location</b><br>Drag to reposition or click anywhere on the map.');

        // Marker drag handler
        userMarker.on('dragend', function (e) {
            const pos = e.target.getLatLng();
            setSearchLocation(pos.lat, pos.lng, null, false);
        });

        // Search Radius Circle
        radiusCircle = L.circle([currentLat, currentLon], {
            radius: currentRadiusKm * 1000,
            color: '#2563eb',
            fillColor: '#3b82f6',
            fillOpacity: 0.12,
            weight: 2,
            dashArray: '4, 6'
        }).addTo(map);

        // Click anywhere on map to reposition search center
        map.on('click', function (e) {
            setSearchLocation(e.latlng.lat, e.latlng.lng, null, false);
        });

        // Try getting user's browser location on initial load
        detectUserLocation();
    }

    // Set Search Center Location and trigger query
    function setSearchLocation(lat, lon, locationName, shouldFly) {
        currentLat = lat;
        currentLon = lon;

        // Update User Marker position
        if (userMarker) {
            userMarker.setLatLng([lat, lon]);
        }

        // Update Radius Circle
        if (radiusCircle) {
            radiusCircle.setLatLng([lat, lon]);
            if (currentRadiusKm > 0) {
                radiusCircle.setRadius(currentRadiusKm * 1000);
                if (!map.hasLayer(radiusCircle)) {
                    radiusCircle.addTo(map);
                }
            } else {
                map.removeLayer(radiusCircle);
            }
        }

        // Update coordinates display
        if (coordsBadge) {
            coordsBadge.textContent = `${lat.toFixed(4)}, ${lon.toFixed(4)}`;
        }

        // Handle panning / flying
        if (shouldFly && map) {
            map.flyTo([lat, lon], getZoomForRadius(currentRadiusKm), {
                duration: 1.2
            });
        }

        // Address reverse geocoding via OpenStreetMap Nominatim
        if (locationName) {
            if (statusText) statusText.textContent = locationName;
            if (userMarker) userMarker.setPopupContent(`<b>Search Location</b><br>${locationName}`).openPopup();
        } else {
            reverseGeocode(lat, lon);
        }

        // Load nearby vehicles for the new search location
        loadNearbyVehicles();
    }

    // Reverse Geocode using OpenStreetMap Nominatim
    async function reverseGeocode(lat, lon) {
        if (locationSpinner) locationSpinner.classList.remove('d-none');
        try {
            const response = await fetch(`/Vehicle/ReverseGeocode?latitude=${lat}&longitude=${lon}`);
            if (response.ok) {
                const data = await response.json();
                const address = data.display_name || `${lat.toFixed(4)}, ${lon.toFixed(4)}`;
                const shortAddress = formatAddress(data) || address;

                if (statusText) statusText.textContent = shortAddress;
                if (userMarker) {
                    userMarker.setPopupContent(`<b>Search Center</b><br><small class="text-muted">${shortAddress}</small>`).openPopup();
                }
            } else {
                if (statusText) statusText.textContent = `${lat.toFixed(4)}, ${lon.toFixed(4)}`;
            }
        } catch (e) {
            console.warn('Reverse geocode error:', e);
            if (statusText) statusText.textContent = `${lat.toFixed(4)}, ${lon.toFixed(4)}`;
        } finally {
            if (locationSpinner) locationSpinner.classList.add('d-none');
        }
    }

    // Format Nominatim address for cleaner display
    function formatAddress(data) {
        if (!data || !data.address) return null;
        const addr = data.address;
        const parts = [];
        if (addr.road || addr.pedestrian || addr.suburb) parts.push(addr.road || addr.pedestrian || addr.suburb);
        if (addr.city || addr.town || addr.village) parts.push(addr.city || addr.town || addr.village);
        if (addr.state) parts.push(addr.state);
        if (addr.country) parts.push(addr.country);
        return parts.join(', ');
    }

    // Search Place or City with OpenStreetMap Nominatim
    async function searchOpenStreetMap(query) {
        if (!query || query.trim().length === 0) return;

        if (locationSpinner) locationSpinner.classList.remove('d-none');
        if (statusText) statusText.textContent = `Searching "${query}" via OpenStreetMap...`;

        try {
            const response = await fetch(`/Vehicle/SearchLocation?query=${encodeURIComponent(query)}`);
            if (response.ok) {
                const results = await response.json();
                if (results && results.length > 0) {
                    const top = results[0];
                    const lat = parseFloat(top.lat);
                    const lon = parseFloat(top.lon);
                    const name = top.display_name.split(',').slice(0, 3).join(',');
                    setSearchLocation(lat, lon, name, true);
                } else {
                    alert(`No locations found on OpenStreetMap matching "${query}". Try another city or landmark.`);
                    if (statusText) statusText.textContent = `${currentLat.toFixed(4)}, ${currentLon.toFixed(4)}`;
                }
            } else {
                alert('OpenStreetMap search failed. Please try again or click directly on the map.');
            }
        } catch (err) {
            console.error('OSM Search error:', err);
            alert('Failed to connect to OpenStreetMap search API.');
        } finally {
            if (locationSpinner) locationSpinner.classList.add('d-none');
        }
    }

    // Create custom vehicle marker icon based on category
    function getVehicleIcon(category) {
        let pinClass = 'pin-sedan';
        let iconClass = 'bi-car-front-fill';

        const cat = (category || '').toLowerCase();
        if (cat.includes('electric')) {
            pinClass = 'pin-electric';
            iconClass = 'bi-lightning-charge-fill';
        } else if (cat.includes('sports')) {
            pinClass = 'pin-sports';
            iconClass = 'bi-speedometer';
        } else if (cat.includes('luxury')) {
            pinClass = 'pin-luxury';
            iconClass = 'bi-gem';
        } else if (cat.includes('suv')) {
            pinClass = 'pin-suv';
            iconClass = 'bi-truck';
        }

        return L.divIcon({
            className: 'custom-vehicle-marker',
            html: `<div class="vehicle-marker-pin ${pinClass}"><i class="bi ${iconClass}"></i></div>`,
            iconSize: [38, 38],
            iconAnchor: [19, 38],
            popupAnchor: [0, -38]
        });
    }

    // Fetch and display nearby vehicles from the backend
    async function loadNearbyVehicles() {
        if (!vehiclesListContainer) return;

        // Loading state
        vehiclesListContainer.innerHTML = `
            <div class="text-center py-5 text-muted">
                <div class="spinner-border spinner-border-sm text-primary mb-2" role="status"></div>
                <div class="small">Searching nearby vehicles on OpenStreetMap...</div>
            </div>`;

        if (foundCountBadge) foundCountBadge.textContent = 'Searching...';

        try {
            const url = `/Vehicle/NearbyVehicles?latitude=${currentLat}&longitude=${currentLon}&radiusKm=${currentRadiusKm}&category=${encodeURIComponent(currentCategory)}`;
            const response = await fetch(url);

            if (!response.ok) {
                throw new Error('Failed to retrieve nearby vehicles.');
            }

            const vehicles = await response.json();
            renderVehicles(vehicles);
        } catch (error) {
            console.error('Error fetching nearby vehicles:', error);
            vehiclesListContainer.innerHTML = `
                <div class="alert alert-danger small m-2">
                    <i class="bi bi-exclamation-triangle me-1"></i> Failed to load vehicles. Please try again.
                </div>`;
            if (foundCountBadge) foundCountBadge.textContent = 'Error loading';
        }
    }

    // Render vehicles on map and sidebar
    function renderVehicles(vehicles) {
        // Clear previous markers
        vehicleMarkersGroup.clearLayers();
        vehicleMarkersMap = {};
        vehiclesListContainer.innerHTML = '';

        const count = vehicles ? vehicles.length : 0;

        // Update counts
        if (foundCountBadge) {
            const radiusLabel = currentRadiusKm > 0 ? `within ${currentRadiusKm} km` : 'worldwide';
            foundCountBadge.textContent = `${count} vehicle${count === 1 ? '' : 's'} available ${radiusLabel}`;
        }
        if (sidebarCountBadge) {
            sidebarCountBadge.textContent = `${count} found`;
        }

        if (count === 0) {
            // Show empty state
            emptyStateContainer.classList.remove('d-none');
            vehiclesListContainer.classList.add('d-none');
            return;
        }

        // Show list
        emptyStateContainer.classList.add('d-none');
        vehiclesListContainer.classList.remove('d-none');

        const mapBounds = [];
        mapBounds.push([currentLat, currentLon]);

        vehicles.forEach((vehicle, index) => {
            const vLat = vehicle.latitude;
            const vLon = vehicle.longitude;

            // Only add to map if vehicle has valid coordinates
            if (vLat !== 0 || vLon !== 0) {
                mapBounds.push([vLat, vLon]);

                const marker = L.marker([vLat, vLon], {
                    icon: getVehicleIcon(vehicle.category),
                    title: vehicle.fullName
                });

                // Rich Leaflet Popup
                const popupContent = `
                    <div class="popup-card">
                        <img src="${vehicle.imageUrl}" alt="${vehicle.fullName}">
                        <div class="popup-body">
                            <div class="d-flex justify-content-between align-items-start mb-1">
                                <span class="badge bg-primary rounded-pill small">${vehicle.category}</span>
                                <span class="text-success fw-bold">₹${Math.round(vehicle.dailyRate).toLocaleString('en-IN')}<small class="text-muted fw-normal">/day</small></span>
                            </div>
                            <h6 class="fw-bold mb-1 text-truncate">${vehicle.fullName}</h6>
                            <div class="d-flex align-items-center gap-2 small text-muted mb-2">
                                <span><i class="bi bi-geo-alt text-danger"></i> <strong>${vehicle.distanceKm} km</strong> away</span>
                                <span>&bull;</span>
                                <span><i class="bi bi-star-fill text-warning"></i> ${vehicle.averageRating}</span>
                            </div>
                            <div class="d-grid gap-1">
                                <a href="/Booking/Create?vehicleId=${vehicle.id}" class="btn btn-primary btn-sm rounded-pill py-1">
                                    <i class="bi bi-calendar-check me-1"></i> Rent Now (₹)
                                </a>
                                <a href="/Vehicle/Details/${vehicle.id}" class="btn btn-outline-secondary btn-sm rounded-pill py-1">
                                    View Full Specs
                                </a>
                            </div>
                        </div>
                    </div>`;

                marker.bindPopup(popupContent);
                vehicleMarkersGroup.addLayer(marker);
                vehicleMarkersMap[vehicle.id] = marker;
            }

            // Create Sidebar Card
            const card = document.createElement('div');
            card.className = 'card vehicle-list-card rounded-4 p-3 mb-2 bg-white';
            card.dataset.vehicleId = vehicle.id;
            card.innerHTML = `
                <div class="d-flex gap-3">
                    <img src="${vehicle.imageUrl}" alt="${vehicle.fullName}" 
                         class="rounded-3 object-fit-cover shadow-sm" 
                         style="width: 86px; height: 86px; min-width: 86px;" />
                    <div class="flex-grow-1 min-w-0">
                        <div class="d-flex justify-content-between align-items-start mb-1">
                            <span class="badge bg-light text-dark border small">${vehicle.category}</span>
                            <span class="text-primary fw-bold">₹${Math.round(vehicle.dailyRate).toLocaleString('en-IN')}<small class="text-muted fw-normal">/d</small></span>
                        </div>
                        <h6 class="fw-bold mb-1 text-truncate" title="${vehicle.fullName}">${vehicle.fullName}</h6>
                        <div class="d-flex align-items-center gap-2 small text-muted mb-2">
                            <span class="badge bg-success-subtle text-success border border-success-subtle rounded-pill">
                                <i class="bi bi-geo-alt-fill"></i> ${vehicle.distanceKm} km
                            </span>
                            <span><i class="bi bi-gear me-1"></i>${vehicle.transmission}</span>
                        </div>
                        <div class="d-flex gap-2">
                            <button type="button" class="btn btn-sm btn-outline-primary rounded-pill py-0 px-2 btn-show-map" data-id="${vehicle.id}">
                                <i class="bi bi-map me-1"></i> Locate
                            </button>
                            <a href="/Booking/Create?vehicleId=${vehicle.id}" class="btn btn-sm btn-primary rounded-pill py-0 px-3">
                                Book
                            </a>
                        </div>
                    </div>
                </div>`;

            // Clicking card or 'Locate' centers map on vehicle
            card.addEventListener('click', function (e) {
                if (e.target.tagName.toLowerCase() === 'a') return; // let book link work normally
                focusVehicleMarker(vehicle.id, vLat, vLon);
            });

            vehiclesListContainer.appendChild(card);
        });

        // Fit map bounds to show vehicles if radius is "Show All" or if map has multiple points
        if (currentRadiusKm === 0 && mapBounds.length > 1) {
            map.fitBounds(mapBounds, { padding: [50, 50] });
        }
    }

    // Focus and highlight a vehicle marker on the OpenStreetMap
    function focusVehicleMarker(vehicleId, lat, lon) {
        if (lat === 0 && lon === 0) {
            alert('This vehicle does not have exact coordinates set.');
            return;
        }

        if (map) {
            map.panTo([lat, lon], { animate: true });
        }

        const marker = vehicleMarkersMap[vehicleId];
        if (marker) {
            marker.openPopup();
        }
    }

    // Detect User Location via Browser HTML5 Geolocation API
    function detectUserLocation() {
        if (!navigator.geolocation) {
            setSearchLocation(21.1702, 72.8311, "Surat, Gujarat (Fleet Base)", true);
            return;
        }

        if (statusText) statusText.textContent = "Detecting your GPS location...";
        if (locationSpinner) locationSpinner.classList.remove('d-none');

        navigator.geolocation.getCurrentPosition(
            function (position) {
                const lat = position.coords.latitude;
                const lon = position.coords.longitude;
                setSearchLocation(lat, lon, null, true);
            },
            function (error) {
                console.warn('Geolocation denied or error:', error.message);
                // Graceful fallback to default fleet base (Surat)
                setSearchLocation(21.1702, 72.8311, "Surat, Gujarat (Fleet Base)", true);
            },
            {
                enableHighAccuracy: true,
                timeout: 8000,
                maximumAge: 60000
            }
        );
    }

    // One-click demo helper: Station sample vehicles around user's location
    async function spawnDemoVehicles() {
        if (locationSpinner) locationSpinner.classList.remove('d-none');
        try {
            const response = await fetch('/Vehicle/SeedDemoVehiclesNear', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    latitude: currentLat,
                    longitude: currentLon
                })
            });

            if (response.ok) {
                const res = await response.json();
                loadNearbyVehicles();
            } else {
                alert('Could not station demo vehicles.');
            }
        } catch (e) {
            console.error('Spawn demo vehicles error:', e);
            alert('Failed to station demo vehicles.');
        } finally {
            if (locationSpinner) locationSpinner.classList.add('d-none');
        }
    }

    // Attach Event Handlers
    function bindEvents() {
        // OpenStreetMap Nominatim Search button
        if (searchBtn && searchInput) {
            searchBtn.addEventListener('click', function () {
                searchOpenStreetMap(searchInput.value);
            });

            searchInput.addEventListener('keydown', function (e) {
                if (e.key === 'Enter') {
                    e.preventDefault();
                    searchOpenStreetMap(searchInput.value);
                }
            });
        }

        // My Location Button
        if (myLocationBtn) {
            myLocationBtn.addEventListener('click', function () {
                detectUserLocation();
            });
        }

        // Search Radius selector change
        if (radiusSelect) {
            radiusSelect.addEventListener('change', function () {
                currentRadiusKm = parseFloat(this.value);
                setSearchLocation(currentLat, currentLon, statusText ? statusText.textContent : null, true);
            });
        }

        // Category filter change
        if (categorySelect) {
            categorySelect.addEventListener('change', function () {
                currentCategory = this.value;
                loadNearbyVehicles();
            });
        }

        // Preset location links
        document.querySelectorAll('.preset-location').forEach(item => {
            item.addEventListener('click', function (e) {
                e.preventDefault();
                const lat = parseFloat(this.dataset.lat);
                const lon = parseFloat(this.dataset.lon);
                const name = this.dataset.name;
                if (searchInput) searchInput.value = name;
                setSearchLocation(lat, lon, name, true);
            });
        });

        // Spawn demo vehicles button
        if (spawnDemoBtn) {
            spawnDemoBtn.addEventListener('click', function () {
                spawnDemoVehicles();
            });
        }

        // Expand radius button
        if (expandRadiusBtn && radiusSelect) {
            expandRadiusBtn.addEventListener('click', function () {
                radiusSelect.value = "100";
                currentRadiusKm = 100;
                setSearchLocation(currentLat, currentLon, statusText ? statusText.textContent : null, true);
            });
        }

        // View Surat fleet base button
        if (sfFleetBtn) {
            sfFleetBtn.addEventListener('click', function () {
                if (searchInput) searchInput.value = "Surat, Gujarat";
                setSearchLocation(21.1702, 72.8311, "Surat, Gujarat (Fleet Base)", true);
            });
        }
    }

    // Initialize on DOM Ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function () {
            initMap();
            bindEvents();
        });
    } else {
        initMap();
        bindEvents();
    }
})();