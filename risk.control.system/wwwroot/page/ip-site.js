function success(position) {
    var lat = position.coords.latitude;
    var long = position.coords.longitude;
    var latlong = lat + "," + long; // Store lat and long in the latlong variable

    // Call fetchIpInfo only after we have the geolocation
    fetchIpInfo(latlong);
}

// Error function to handle geolocation failure
function error() {
    console.error('Geolocation request failed or was denied.');
    // You may want to handle a default location or notify the user here
    fetchIpInfo(); // Optionally, still send the request even without location data
}
async function fetchIpInfo(latlong) {
    try {
        if (latlong) {

            // Prepare the URL with the latlong parameter if available
            const url = "/api/Notification/GetClientIp?url=" + encodeURIComponent(window.location.pathname) + "&latlong=" + encodeURIComponent(latlong);

            // Make the fetch call
            const response = await fetch(url);

            // Handle if the response is not OK
            if (!response.ok) {
                console.error('There has been a problem with your ip fetch operation');
                document.querySelector('#country .info-data').textContent = 'Not available';
                document.querySelector('#region .info-data').textContent = 'Not available';
                document.querySelector('#city .info-data').textContent = data.city || 'Not available';
                document.querySelector('#postCode .info-data').textContent = 'Not available';
                document.querySelector('#isp .info-data').textContent = 'Not available';
                document.querySelector('#maps .info-data #location-map').src ='/img/no-map.jpeg';
            }
            else {
                // Parse the response data as JSON
                const data = await response.json();
                // Update each element with the respective data
                document.querySelector('#ipAddress .info-data').textContent = data.ipAddress || 'Not available';
                document.querySelector('#country .info-data').textContent = data.country || 'Not available';
                document.querySelector('#region .info-data').textContent = data.region || 'Not available';
                document.querySelector('#city .info-data').textContent = data.district || data.city || 'Not available';
                document.querySelector('#postCode .info-data').textContent = data.postCode || 'Not available';
                document.querySelector('#isp .info-data').textContent = data.isp || 'Not available';
                document.querySelector('#location-map').src = data.mapUrl || '/img/no-map.jpeg';
            }
        }

    } catch (error) {
        console.error('There has been a problem with your fetch operation:', error);
    }
}

//if (navigator.geolocation) {
//    navigator.geolocation.getCurrentPosition(success, error);
//} else {
//    console.error('Geolocation is not supported by this browser.');
//}
