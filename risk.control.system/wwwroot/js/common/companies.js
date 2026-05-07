$(function () {
    const countryCode = $('#countryCode').val();
    const isdCode = $('#Isd').val();
    displayBankCodeLabels(countryCode, isdCode);

    $('#edit-company').on('click', function () {
        $("body").addClass("submit-progress-bg");
        // Wrap in setTimeout so the UI
        // can update the spinners
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);
        // Disable all buttons, submit inputs, and anchors
        $('button, input[type="submit"], a').prop('disabled', true);

        // Add a class to visually indicate disabled state for anchors
        $('a').addClass('disabled-anchor').on('click', function (e) {
            e.preventDefault(); // Prevent default action for anchor clicks
        });
        $('#edit-company').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Edit Company");

        var article = document.getElementById("article");
        if (article) {
            var nodes = article.getElementsByTagName('*');
            for (var i = 0; i < nodes.length; i++) {
                nodes[i].disabled = true;
            }
        }
    });

    $('#edit-profile').on('click', function () {
        $("body").addClass("submit-progress-bg");
        // Wrap in setTimeout so the UI
        // can update the spinners
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);
        // Disable all buttons, submit inputs, and anchors
        $('button, input[type="submit"], a').prop('disabled', true);

        // Add a class to visually indicate disabled state for anchors
        $('a').addClass('disabled-anchor').on('click', function (e) {
            e.preventDefault(); // Prevent default action for anchor clicks
        });
        $('#edit-profile').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Edit Profile");

        var article = document.getElementById("article");
        if (article) {
            var nodes = article.getElementsByTagName('*');
            for (var i = 0; i < nodes.length; i++) {
                nodes[i].disabled = true;
            }
        }
    });

    $('#editagency').on('click', function () {
        $("body").addClass("submit-progress-bg");
        // Wrap in setTimeout so the UI
        // can update the spinners
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);
        // Disable all buttons, submit inputs, and anchors
        $('button, input[type="submit"], a').prop('disabled', true);

        // Add a class to visually indicate disabled state for anchors
        $('a').addClass('disabled-anchor').on('click', function (e) {
            e.preventDefault(); // Prevent default action for anchor clicks
        });
        $('#editagency').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Edit Profile");

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
        // Disable all buttons, submit inputs, and anchors
        $('button, input[type="submit"], a').prop('disabled', true);

        // Add a class to visually indicate disabled state for anchors
        $('a').addClass('disabled-anchor').on('click', function (e) {
            e.preventDefault(); // Prevent default action for anchor clicks
        });
        var userCount = $('#UserCount').val();
        $('.btn.btn-success').html(`<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Users (${userCount})`);

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
        // Disable all buttons, submit inputs, and anchors
        $('button, input[type="submit"], a').prop('disabled', true);

        // Add a class to visually indicate disabled state for anchors
        $('a').addClass('disabled-anchor').on('click', function (e) {
            e.preventDefault(); // Prevent default action for anchor clicks
        });
        var serviceCount = $('#ServiceCount').val();
        $('.btn.btn-danger').html(`<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Service (${serviceCount})`);

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
            img.attr("src", "/img/FilledStar.jpeg").prevAll("img.main-rating").attr("src", "/img/FilledStar.jpeg");
        }
    });

    var agencyRating = $('#agency-rating');
    if (agencyRating && agencyRating.length > 0) {
        refillRating($('#agency-rating'));
    }
});

function showedit() {
    $("body").addClass("submit-progress-bg");
    // Wrap in setTimeout so the UI
    // can update the spinners
    setTimeout(function () {
        $(".submit-progress").removeClass("hidden");
    }, 1);
    // Disable all buttons, submit inputs, and anchors
    $('button, input[type="submit"], a').prop('disabled', true);

    // Add a class to visually indicate disabled state for anchors
    $('a').addClass('disabled-anchor').on('click', function (e) {
        e.preventDefault(); // Prevent default action for anchor clicks
    });
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
    // Disable all buttons, submit inputs, and anchors
    $('button, input[type="submit"], a').prop('disabled', true);

    // Add a class to visually indicate disabled state for anchors
    $('a').addClass('disabled-anchor').on('click', function (e) {
        e.preventDefault(); // Prevent default action for anchor clicks
    });
    $('a.btn.btn-info').html("<i class='fas fa-sync fa-spin'></i> Roles");

    var article = document.getElementById("article");
    if (article) {
        var nodes = article.getElementsByTagName('*');
        for (var i = 0; i < nodes.length; i++) {
            nodes[i].disabled = true;
        }
    }
}

function displayBankCodeLabels(countryCode, isdCode) {
    countryCode = (countryCode || '').toUpperCase().trim();
    isdCode = (isdCode || '').trim();

    const isIndia = (countryCode === 'IN' || isdCode === '91');
    const isAustralia = (countryCode === 'AU' || isdCode === '61');

    if (isAustralia) {
        $('.info-box-text:contains("IFSC Code")').text('BSB Code');
    } else if (!isIndia) {
        $('.info-box-text:contains("IFSC Code")').text('Bank Code');
    }
}

function refillRating(container) {
    var avgRating = parseFloat(container.find("span.avr").text()) || 0;
    $('#agency-rating img.rating').each(function () {
        var currentStar = parseInt($(this).attr('id'));
        $(this).attr("src", currentStar <= avgRating ? "/img/FilledStar.jpeg" : "/img/StarFade.gif");
    });

    var av = parseFloat($('.avr').text()); // Get the average rating from the page

    if (!isNaN(av) && av > 0) {  // Ensure it's a valid rating
        var stars = $("#agency-rating img.rating"); // Get all star images in the row

        // Loop through each star and highlight them based on the average rating
        stars.each(function (index) {
            var star = $(this);

            if (index < Math.floor(av)) {  // Fully filled stars
                star.attr("src", "/img/FilledStar.jpeg");
            } else {
                star.attr("src", "/img/StarFade.gif");  // Faded stars
            }
        });
    }
}