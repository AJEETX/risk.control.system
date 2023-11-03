(g => { var h, a, k, p = "API", c = "google", l = "importLibrary", q = "__ib__", m = document, b = window; b = b[c] || (b[c] = {}); var d = b.maps || (b.maps = {}), r = new Set, e = new URLSearchParams, u = () => h || (h = new Promise(async (f, n) => { await (a = m.createElement("script")); e.set("libraries", [...r] + ""); for (k in g) e.set(k.replace(/[A-Z]/g, t => "_" + t[0].toLowerCase()), g[k]); e.set("callback", c + ".maps." + q); a.src = `https://maps.${c}apis.com/maps/api/js?` + e; d[q] = f; a.onerror = () => h = n(Error(p + " could not load.")); a.nonce = m.querySelector("script[nonce]")?.nonce || ""; m.head.append(a) })); d[l] ? console.warn(p + " only loads once. Ignoring:", g) : d[l] = (f, ...n) => r.add(f) && u().then(() => d[l](f, ...n)) })
    ({ key: "AIzaSyDH8T9FvJ8n2LNwxkppRAeOq3Mx7I3qi1E", v: "beta" });

async function initMap(url) {
    var response = $.ajax({
        type: "GET",
        url: url,
        async: false
    }).responseText;
    const data = JSON.parse(response);

    // Request needed libraries.
    const { Map } = await google.maps.importLibrary("maps");
    const { AdvancedMarkerElement } = await google.maps.importLibrary("marker");
    const { LatLng } = await google.maps.importLibrary("core");
    var bounds = new google.maps.LatLngBounds();

    const center = new LatLng(data.lat, data.lng);
    const map = new Map(document.getElementById("map"), { mapTypeId: google.maps.MapTypeId.ROADMAP, mapId: "4504f8b37365c3d0" });

    map.setCenter(center);

    for (const property of data.response) {
        var latLng = new google.maps.LatLng(property.position.lat, property.position.lng);

        const AdvancedMarkerElement = new google.maps.marker.AdvancedMarkerElement({
            map,
            content: buildContent(property),
            position: latLng,
            title: property.description,
        });

        AdvancedMarkerElement.addListener("click", () => {
            toggleHighlight(AdvancedMarkerElement, property);
        });
        bounds.extend(latLng);
    }

    google.maps.event.addListenerOnce(map, 'bounds_changed', function (event) {
        this.setZoom(map.getZoom() - 1);

        if (this.getZoom() > 15) {
            this.setZoom(15);
        }
    });

    map.fitBounds(bounds);
    map.setCenter(bounds.getCenter());
    map.setZoom(map.getZoom() - 1);
    if (map.getZoom() > 15) {
        map.setZoom(15);
    }
}

function toggleHighlight(markerView, property) {
    if (markerView.content.classList.contains("highlight")) {
        markerView.content.classList.remove("highlight");
        markerView.zIndex = null;
    } else {
        markerView.content.classList.add("highlight");
        markerView.zIndex = 1;
    }
}

function buildContent(property) {
    const content = document.createElement("div");

    content.classList.add("property");
    content.innerHTML = `
                                        <div class="icon">
                                            <i aria-hidden="true" class="fa fa-icon fa-${property.type}" title="${property.type}"></i>
                                            <span class="fa-sr-only">${property.type}</span>
                                        </div>
                                        <div class="details">
                                            <div class="price">$ ${property.price}</div>
                                            <div class="address"><i aria-hidden="true" class="fas fa-home" title="bedroom"></i> ${property.address}</div>
                                            <div class="features">
                                            <div>
                                                        <i aria-hidden="true" class="fas fa-rupee-sign" title="bedroom"></i>
                                                <span class="fa-sr-only">bedroom</span>
                                                <span>${property.bed}</span>
                                            </div>
                                            <div>
                                                        <i aria-hidden="true" class="fas fa-phone-square-alt" title="bathroom"></i>
                                                <span class="fa-sr-only">bathroom</span>
                                                <span>${property.bath}</span>
                                            </div>
                                            <div>
                                                <i aria-hidden="true" class="fa fa-ruler fa-lg size" title="size"></i>
                                                <span class="fa-sr-only">size</span>
                                                <span>${property.size} <sup>2</sup></span>
                                            </div>
                                            </div>
                                        </div>
                                        `;
    return content;
}