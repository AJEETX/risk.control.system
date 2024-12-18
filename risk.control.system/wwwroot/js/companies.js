var apiKey = 'AIzaSyCYPyGotbPJAcE9Ap_ATSKkKOrXCQC4ops';
$(function () {

    $('a#edit-profile.btn.btn-warning').on('click', function () {
        $("body").addClass("submit-progress-bg");
        // Wrap in setTimeout so the UI
        // can update the spinners
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);
        $('.btn').attr('disabled', 'disabled');
        $('a#edit-profile.btn.btn-warning').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Profile");

        var article = document.getElementById("article");
        if (article) {
            var nodes = article.getElementsByTagName('*');
            for (var i = 0; i < nodes.length; i++) {
                nodes[i].disabled = true;
            }
        }
    });

    $('#edit-company.btn.btn-warning').on('click', function () {
        $("body").addClass("submit-progress-bg");
        // Wrap in setTimeout so the UI
        // can update the spinners
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);
        $('#edit-company.btn.btn-warning').attr('disabled', 'disabled');
        $('#edit-company.btn.btn-warning').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Edit Company");

        var article = document.getElementById("article");
        if (article) {
            var nodes = article.getElementsByTagName('*');
            for (var i = 0; i < nodes.length; i++) {
                nodes[i].disabled = true;
            }
        }
    });
    $('#edit-profile.btn.btn-warning').on('click', function () {
        $("body").addClass("submit-progress-bg");
        // Wrap in setTimeout so the UI
        // can update the spinners
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);
        $('#edit-profile.btn.btn-warning').attr('disabled', 'disabled');
        $('#edit-profile.btn.btn-warning').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Edit Profile");

        var article = document.getElementById("article");
        if (article) {
            var nodes = article.getElementsByTagName('*');
            for (var i = 0; i < nodes.length; i++) {
                nodes[i].disabled = true;
            }
        }
    });

    $('a#editagency.btn.btn-warning').on('click', function () {
        $("body").addClass("submit-progress-bg");
        // Wrap in setTimeout so the UI
        // can update the spinners
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);
        $('.btn').attr('disabled', 'disabled');
        $('a#editagency.btn.btn-warning').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Edit Agency");

        var article = document.getElementById("article");
        if (article) {
            var nodes = article.getElementsByTagName('*');
            for (var i = 0; i < nodes.length; i++) {
                nodes[i].disabled = true;
            }
        }
    });

    $('.btn.btn-success').on('click', function () {
        $("body").addClass("submit-progress-bg");
        // Wrap in setTimeout so the UI
        // can update the spinners
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);
        $('.btn').attr('disabled', 'disabled');
        $('.btn.btn-success').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> User");

        var article = document.getElementById("article");
        if (article) {
            var nodes = article.getElementsByTagName('*');
            for (var i = 0; i < nodes.length; i++) {
                nodes[i].disabled = true;
            }
        }
    });


    $('.btn.btn-danger').on('click', function () {
        $("body").addClass("submit-progress-bg");
        // Wrap in setTimeout so the UI
        // can update the spinners
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);
        $('.btn').attr('disabled', 'disabled');
        $('.btn.btn-danger').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Service");

        var article = document.getElementById("article");
        if (article) {
            var nodes = article.getElementsByTagName('*');
            for (var i = 0; i < nodes.length; i++) {
                nodes[i].disabled = true;
            }
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
        "/images/beachflag.png";
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
    var b_marker;
    if (b_current_data && b_current_data.results && b_current_data.results.length > 0 && b_current_data.results[0].formatted_address) {
        b_marker = new google.maps.Marker({
            position: b_LatLng,
            title: "Branch Location: " + b_current_data.results[0].formatted_address
        });
    } else {
        b_marker = new google.maps.Marker({
            position: b_LatLng,
            title: "Branch Location: Location Unknown"
        });
    }
    var b_getInfoWindow;
    b_marker.setMap(map);
    if (b_current_data && b_current_data.results && b_current_data.results.length > 0 && b_current_data.results[0].formatted_address) {
        b_getInfoWindow = new google.maps.InfoWindow({
            content: "<b>Branch Location</b><br/> " +
                b_current_data.results[0].formatted_address + ""
        });
    }
    else {
        b_getInfoWindow = new google.maps.InfoWindow({
            content: "<b>Branch Location</b><br/> " +
                "Location Unknown"
        });
    }
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
        title: current_data && current_data.results && current_data.results.length > 0 && current_data.results[0].formatted_address ? "You are here: " + current_data.results[0].formatted_address :"Location Unknown"
    });

    marker.setMap(map);
    var getInfoWindow = new google.maps.InfoWindow({
        content: "<b>Your Current Location</b><br/> " +
            current_data && current_data.results && current_data.results.length > 0 && current_data.results[0].formatted_address ? current_data.results[0].formatted_address + "" :"Location Unknown"
    });
    getInfoWindow.open(map, marker);

    bounds.extend(LatLng);
    bounds.extend(b_LatLng);

    map.fitBounds(bounds);
    map.setCenter(bounds.getCenter());
}

function showedit() {
    $("body").addClass("submit-progress-bg");
    // Wrap in setTimeout so the UI
    // can update the spinners
    setTimeout(function () {
        $(".submit-progress").removeClass("hidden");
    }, 1);
    $('a.btn.btn-warning').attr('disabled', 'disabled');
    $('a.btn.btn-warning').html("<i class='fas fa-sync fa-spin'></i> Edit");

    var article = document.getElementById("article");
    if (article) {
        var nodes = article.getElementsByTagName('*');
        for (var i = 0; i < nodes.length; i++) {
            nodes[i].disabled = true;
        }
    }
}

function showdetails() {
    $("body").addClass("submit-progress-bg");
    // Wrap in setTimeout so the UI
    // can update the spinners
    setTimeout(function () {
        $(".submit-progress").removeClass("hidden");
    }, 1);
    $('a.btn.btn-info').attr('disabled', 'disabled');
    $('a.btn.btn-info').html("<i class='fas fa-sync fa-spin'></i> Roles");

    var article = document.getElementById("article");
    if (article) {
        var nodes = article.getElementsByTagName('*');
        for (var i = 0; i < nodes.length; i++) {
            nodes[i].disabled = true;
        }
    }
}
GetLoc();

