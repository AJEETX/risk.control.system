var apiKey = 'AIzaSyDH8T9FvJ8n2LNwxkppRAeOq3Mx7I3qi1E';


$(function () {

    $('#create-policy').on('click', function () {
        $("body").addClass("submit-progress-bg");
        // Wrap in setTimeout so the UI
        // can update the spinners
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);
        $('#create-policy').attr('disabled', 'disabled');
        $('#create-policy').html("<i class='far fa-file-powerpoint' aria-hidden='true'></i> Add Policy .....");

        var nodes = document.getElementById("article").getElementsByTagName('*');
        for (var i = 0; i < nodes.length; i++) {
            nodes[i].disabled = true;
        }
    });


    $("#agency-rating").each(function () {
        var av = $(this).find("span.avr").text();

        if (av != "" || av != null) {
            var img = $(this).find("img[id='" + parseInt(av) + "']");
            img.attr("src", "/images/FilledStar.jpeg").prevAll("img.main-rating").attr("src", "/images/FilledStar.jpeg");
        }
    });
});
function GetLoc() {
    if (navigator.geolocation) {
        navigator.geolocation.getCurrentPosition(success);
    } else {
        alert("There is Some Problem on your current browser to get Geo Location!");
    }
}

async function success(position) {
    const image =
        "../images/beachflag.png";
    const { Map, InfoWindow } = await google.maps.importLibrary("maps");
    // The location of Uluru
    var element = document.getElementById("company-map");
    var bounds = new google.maps.LatLngBounds();

    // The map, centered at branch
    var map = new Map(element, {
        scaleControl: true,
        zoom: 14,
        center: position,
        mapId: "4504f8b37365c3d0",
        mapTypeId: google.maps.MapTypeId.ROADMAP,
    });
    var b_lat = parseFloat($('#Latitude').val());
    var b_lng = parseFloat($('#Longitude').val());
    var b_response = $.ajax({
        type: "GET",
        url: `https://maps.googleapis.com/maps/api/geocode/json?latlng=${b_lat},${b_lng}&sensor=false&key=${apiKey}`,
        async: false
    }).responseText;

    var b_current_data = JSON.parse(b_response);

    var b_LatLng = new google.maps.LatLng(b_lat, b_lng);
    var b_marker = new google.maps.Marker({
        position: b_LatLng,
        title: "Branch Location: " + b_current_data.results[0].formatted_address
    });
    b_marker.setMap(map);
    var b_getInfoWindow = new google.maps.InfoWindow({
        content: "<b>Branch Location</b><br/> " +
            b_current_data.results[0].formatted_address + ""
    });
    b_getInfoWindow.open(map, b_marker);

    var lat = parseFloat(position.coords.latitude);
    var long = parseFloat(position.coords.longitude);
    var locresponse = $.ajax({
        type: "GET",
        url: `https://maps.googleapis.com/maps/api/geocode/json?latlng=${lat},${long}&sensor=false&key=${apiKey}`,
        async: false
    }).responseText;
    var current_data = JSON.parse(locresponse);

    var LatLng = new google.maps.LatLng(lat, long);
    var marker = new google.maps.Marker({
        position: LatLng,
        icon: image,
        title: "You are here: " + current_data.results[0].formatted_address
    });

    marker.setMap(map);
    var getInfoWindow = new google.maps.InfoWindow({
        content: "<b>Your Current Location</b><br/> " +
            current_data.results[0].formatted_address + ""
    });
    getInfoWindow.open(map, marker);

    bounds.extend(LatLng);
    bounds.extend(b_LatLng);

    map.fitBounds(bounds);
    map.setCenter(bounds.getCenter());
}

GetLoc();

