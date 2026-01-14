$(document).ready(function () {

    const countryCode = $('#countryCode').val();
    const isdCode = $('#Isd').val();

    const isIndia = (countryCode === 'IN' || isdCode === '91');
    const isAustralia = (countryCode === 'AU' || isdCode === '61');

    if (isAustralia) {
        $('.info-box-text:contains("IFSC Code")').text('BSB Code');
    } else if (!isIndia) {
        $('.info-box-text:contains("IFSC Code")').text('Bank Code');
    }

    // Initialize existing rating display
    refillRating($('#agency-rating'));

    $('#agency-rating').on('mouseover', 'img.rating', function () {
        var starImage = $(this);
        starImage.addClass("toggle-password-visibility");
        var starId = starImage.attr('id'); // Get the ID of the hovered star
        var rating = parseInt(starId); // Convert to integer
        var av = parseFloat($('.avr').text());

        // Update stars to reflect the hover rating
        $('#agency-rating img.rating').each(function (index) {
            if (index < rating) {
                $(this).attr('src', '/img/FilledStar.jpeg');
            } else if (index === rating && av % 1 !== 0) {
                $(this).attr('src', '/img/HalfStar.jpeg');
            } else {
                $(this).attr('src', '/img/StarFade.gif');
            }
        });
    }).on('mouseleave', 'img.rating', function () {
        var starImage = $(this);
        starImage.removeClass("toggle-password-visibility");
        // Reset stars to original state when mouse leaves
        // You can call the same function as above to reset the stars to the original rating state
        var av = parseFloat($('.avr').text());
        $('#agency-rating img.rating').each(function (index) {
            if (index < Math.floor(av)) {
                $(this).attr("src", "/img/FilledStar.jpeg");
            } else if (index === Math.floor(av) && av % 1 !== 0) {
                $(this).attr("src", "/img/HalfStar.jpeg");
            } else {
                $(this).attr("src", "/img/StarFade.gif");
            }
        });
    });

    $('#agency-rating').on('click', 'img.rating', function (e) {
        var starId = $(this).attr('id');
        var vendorId = $('#vendorId').val();

        console.log('Rated ' + starId + ' stars for vendor ' + vendorId);
        var url = "/Vendors/PostRating?rating=" + starId + "&mid=" + vendorId;

        $.post(url, null, function (data) {
            $("#rating-result-data").text(data);
            refillRating($('#agency-rating')); // Update rating display after click
        });
    });

    function highlightStars(rating) {
        $('#agency-rating img.rating').each(function () {
            var currentStar = parseInt($(this).attr('id'));
            $(this).attr("src", currentStar <= rating ? "/img/FilledStar.jpeg" : "/img/StarFade.gif");
        });
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
                } else if (index === Math.floor(av) && av % 1 !== 0) {  // Handle half-filled stars
                    star.attr("src", "/img/HalfStar.jpeg");  // You need a half-star image
                } else {
                    star.attr("src", "/img/StarFade.gif");  // Faded stars
                }
            });
        }
    }
});
