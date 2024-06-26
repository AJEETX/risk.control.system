//loadScript();
//function initialize() {

//    if (navigator.geolocation) {
//        navigator.geolocation.getCurrentPosition(success);
//    }
//    else {
//        fetchIpInfo();
//    }
//}
var hexData = 'AIzaSyDYRB1qIx1AyTxGnV5r5IZC3mk4uYV6MFI';

function loadScript() {
    var script = document.createElement('script');
    script.type = 'text/javascript';
    script.src = "https://maps.googleapis.com/maps/api/js?key=" + hexData + "&sensor=false&callback=initialize";
    document.body.appendChild(script);
}
async function success(position) {
    const { Map, InfoWindow } = await google.maps.importLibrary("maps");
    const { AdvancedMarkerElement } = await google.maps.importLibrary("marker");
    var bounds = new google.maps.LatLngBounds();
    var lat = position.coords.latitude;
    var long = position.coords.longitude;
    var center = {
        lat: position.coords.latitude,
        lng: position.coords.longitude
    };
    const response = await fetch(`https://maps.googleapis.com/maps/api/geocode/json?latlng=${lat},${long}&sensor=false&key=${hexData}`);
    const mapUrlData = await response.json();


    var LatLng = new google.maps.LatLng(lat, long);
    var mapOptions = {
        center: LatLng,
        zoom: 14,
        mapId: "4504f8b37365c3d0",
        mapTypeId: google.maps.MapTypeId.ROADMAP
    };

    var map = new google.maps.Map(document.getElementById("maps"), mapOptions);
    var marker = new google.maps.Marker({
        icon: "../images/beachflag.png",
        position: LatLng,
        title: "You are here: " + mapUrlData.results[0].formatted_address
    });

    marker.setMap(map);
    var getInfoWindow = new google.maps.InfoWindow({
        content: "<b>Your Current Location</b><br/> " +
            mapUrlData.results[0].formatted_address + ""
    });
    getInfoWindow.open(map, marker);
    map.fitBounds(bounds);
    map.setCenter(bounds.getCenter());
}
async function fetchIpInfo() {
    try {
        const url = "/api/Notification/GetClientIp?url="+ encodeURIComponent(window.location.pathname);
        //const url = "/api/Notification/GetClientIp";
        const response = await fetch(url);
        if (!response.ok) {
            throw new Error('Network response was not ok');
        }
        const data = await response.json();
        // Update each element with the respective data
        document.querySelector('#ipAddress .info-data').textContent = data.ipAddress || 'Not available';
        document.querySelector('#country .info-data').textContent = data.country || 'Not available';
        document.querySelector('#region .info-data').textContent = data.region || 'Not available';
        document.querySelector('#city .info-data').textContent = data.city || 'Not available';
        document.querySelector('#postCode .info-data').textContent = data.postCode || 'Not available';
        document.querySelector('#isp .info-data').textContent = data.isp || 'Not available';
        document.querySelector('#latLong .info-data').textContent = data.longitude ? data.latitude + "/" + data.longitude : 'Not available';
        document.querySelector('#maps .info-data #location-map').src = data.mapUrl || '/img/no-map.jpeg';

    } catch (error) {
        console.error('There has been a problem with your fetch operation:', error);
    }
}
window.onload = fetchIpInfo;
