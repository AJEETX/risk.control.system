var map;
var markers = [];
function haversine_distance(mk1, mk2) {
    var R = 3958.8; // Radius of the Earth in miles
    var rlat1 = mk1.position.lat() * (Math.PI / 180); // Convert degrees to radians
    var rlat2 = mk2.position.lat() * (Math.PI / 180); // Convert degrees to radians
    var difflat = rlat2 - rlat1; // Radian difference (latitudes)
    var difflon = (mk2.position.lng() - mk1.position.lng()) * (Math.PI / 180); // Radian difference (longitudes)

    var d = 2 * R * Math.asin(Math.sqrt(Math.sin(difflat / 2) * Math.sin(difflat / 2) + Math.cos(rlat1) * Math.cos(rlat2) * Math.sin(difflon / 2) * Math.sin(difflon / 2)));
    return d * 1.609;
}
function initReportMap() {
    var claimId = document.getElementById('claimId').value;
    var center = {
        lat: 40.774102,
        lng: -73.971734
    };
    var dakota = {
        lat: 40.7767644,
        lng: -73.9761399
    };
    var frick = {
        lat: 40.771209,
        lng: -73.9673991
    };
    var url;
    var distance;
    var duration;
    //AGENT ID
    var response = $.ajax({
        type: "GET",
        url: "/api/ClaimsInvestigation/GetAgentDetail?claimId=" + claimId,
        async: false
    }).responseText;
    if (response) {
        var data = JSON.parse(response);
        if (data && data.center && data.dakota && data.frick && data.url && data.distance && data.duration) {

            var faceMapUrl = document.getElementById('agent-map-url');
            if (faceMapUrl) {
                faceMapUrl.src = data.url;
            }
            var faceMsg = document.getElementById('agent-msg');
            if (faceMsg) {
                faceMsg.innerHTML = `<p>Distance: <em> ${data.distance} </em>, Duration <em>${data.duration}</em>.</p>`;
            }


            //center = data.center;
            //dakota = data.dakota;
            //frick = data.frick,
            //url = data.url,
            //    distance = data.distance,
            //duration = data.duration
        }
    }
    //FACE ID
    var response = $.ajax({
        type: "GET",
        url: "/api/ClaimsInvestigation/GetFaceDetail?claimId=" + claimId,
        async: false
    }).responseText;
    if (response) {
        var data = JSON.parse(response);
        if (data && data.center && data.dakota && data.frick && data.url && data.distance && data.duration) {

            var faceMapUrl = document.getElementById('face-map-url');
            if (faceMapUrl) {
                faceMapUrl.src = data.url;
            }
            var faceMsg = document.getElementById('face-msg');
            if (faceMsg) {
                faceMsg.innerHTML = `<p>Distance: <em> ${data.distance} </em>, Duration <em>${data.duration}</em>.</p>`;
            }
            

            //center = data.center;
            //dakota = data.dakota;
            //frick = data.frick,
            //url = data.url,
            //    distance = data.distance,
            //duration = data.duration
        }
    }
    
    //if (faceHtml && faceMsgHtml) {
    //    initLocationMap(center, dakota, frick, faceHtml , faceMsgHtml);
    //}

    // PAN ID
    var ocrResponse = $.ajax({
        type: "GET",
        url: "/api/ClaimsInvestigation/GetOcrDetail?claimId=" + claimId,
        async: false
    }).responseText;
    if (ocrResponse) {
        var data = JSON.parse(ocrResponse);
        if (data && data.center && data.dakota && data.frick && data.url && data.distance && data.duration) {

            var ocrMapUrl = document.getElementById('ocr-map-url');
            if (ocrMapUrl) {
                ocrMapUrl.src = data.url;
            }
            var ocrMsg = document.getElementById('ocr-msg');
            if (ocrMsg) {
                ocrMsg.innerHTML = `<p>Distance: <em> ${data.distance} </em>, Duration <em>${data.duration}</em>.</p>`;
            }

            //center = odata.center;
            //dakota = odata.dakota;
            //frick = odata.frick
        }
    }
    //var ocrHtml = document.getElementById('ocr-map');
    //var ocrMsgHtml = document.getElementById('ocr-msg');
    //if (ocrHtml && ocrMsgHtml) {
    //    initLocationMap(center, dakota, frick, ocrHtml, ocrMsgHtml);
    //}

    //PASSPORT ID
    var passportResponse = $.ajax({
        type: "GET",
        url: "/api/ClaimsInvestigation/GetPassportDetail?claimId=" + claimId,
        async: false
    }).responseText;
    if (passportResponse) {
        var pdata = JSON.parse(passportResponse);
        if (pdata && pdata.center && pdata.dakota && pdata.frick) {
            center = pdata.center;
            dakota = pdata.dakota;
            frick = pdata.frick
        }
    }
    var passportHtml = document.getElementById('passport-map');
    var passportMsgHtml = document.getElementById('passport-msg');
    if (passportHtml && passportMsgHtml) {
        initLocationMap(center, dakota, frick, passportHtml, passportMsgHtml);
    }

    //AUDIO ID
    var audioResponse = $.ajax({
        type: "GET",
        url: "/api/ClaimsInvestigation/GetAudioDetail?claimId=" + claimId,
        async: false
    }).responseText;
    if (audioResponse) {
        var pdata = JSON.parse(audioResponse);
        if (pdata && pdata.center && pdata.dakota && pdata.frick) {
            center = pdata.center;
            dakota = pdata.dakota;
            frick = pdata.frick
        }
    }
    var audioHtml = document.getElementById('audio-map');
    var audioMsgHtml = document.getElementById('audio-msg');
    if (audioHtml && audioMsgHtml) {
        initLocationMap(center, dakota, frick, audioHtml, audioMsgHtml);
    }

    //VIDEO ID
    var videoResponse = $.ajax({
        type: "GET",
        url: "/api/ClaimsInvestigation/GetVideoDetail?claimId=" + claimId,
        async: false
    }).responseText;
    if (videoResponse) {
        var pdata = JSON.parse(videoResponse);
        if (pdata && pdata.center && pdata.dakota && pdata.frick) {
            center = pdata.center;
            dakota = pdata.dakota;
            frick = pdata.frick
        }
    }
    var videoHtml = document.getElementById('video-map');
    var videoMsgHtml = document.getElementById('video-msg');
    if (videoHtml && videoMsgHtml) {
        initLocationMap(center, dakota, frick, videoHtml, videoMsgHtml);
    }
}

function initLocationMap(center, dakota, frick, mapHtml, msgHtml) {
    const options = {
        scaleControl: true,
        center: center,
        mapId: "4504f8b37365c3d0",
        mapTypeId: google.maps.MapTypeId.ROADMAP,
    };

    // Create or reuse the map object
    map = new google.maps.Map(mapHtml, options);

    // Clear previous markers
    clearMarkers();

    // The markers for The Dakota and The Frick Collection
    var mk1 = new google.maps.Marker({
        position: dakota,
        map: map
    });
    var mk2 = new google.maps.Marker({
        position: frick,
        map: map
    });

    // Add the markers to the global markers array
    markers.push(mk1, mk2);

    var bounds = new google.maps.LatLngBounds();
    bounds.extend(dakota);
    bounds.extend(frick);

    // Draw a line showing the straight distance between the markers
    var line = new google.maps.Polyline({ path: [dakota, frick], map: map });

    // Calculate and display the distance between markers
    var distance = haversine_distance(mk1, mk2);
    msgHtml.innerHTML = "Distance between expected vs visited location: " + distance.toFixed(2) + " km.";

    let directionsService = new google.maps.DirectionsService();
    let directionsRenderer = new google.maps.DirectionsRenderer();
    directionsRenderer.setMap(map); // Existing map object displays directions

    // Create route from existing points used for markers
    const route = {
        origin: dakota,
        destination: frick,
        travelMode: 'DRIVING'
    }

    directionsService.route(route, function (response, status) { // anonymous function to capture directions
        if (status !== 'OK') {
            console.log('Directions request failed due to ' + status);
            return;
        } else {
            directionsRenderer.setDirections(response); // Add route to the map
            var directionsData = response.routes[0].legs[0]; // Get data about the mapped route
            if (!directionsData) {
                window.alert('Directions request failed');
                return;
            } else {
                msgHtml.innerHTML += "<br>";
                msgHtml.innerHTML += " Driving distance is " + directionsData.distance.text + " (" + directionsData.duration.text + ").";
            }
        }
    });

    map.fitBounds(bounds);
    map.setCenter(bounds.getCenter());
    map.setZoom(map.getZoom() - 1);
    if (map.getZoom() > 18) {
        map.setZoom(18);
    }
}

function clearMarkers() {
    // Remove each marker from the map and from the markers array
    for (let i = 0; i < markers.length; i++) {
        markers[i].setMap(null);
    }
    markers = []; // Clear the array of markers
}