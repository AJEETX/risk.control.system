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
        if (!starImage.data('bs.tooltip')) {
            // Set the title attribute for the tooltip text
            starImage.attr("title", "Rate this agency");

            // Set data-toggle to enable the tooltip
            starImage.attr("data-toggle", "tooltip");
            new bootstrap.Tooltip(starImage[0]);
        }
        starImage.addClass("toggle-password-visibility");
        var starId = starImage.attr('id'); // Get the ID of the hovered star
        var rating = parseInt(starId); // Convert to integer
        var av = parseFloat($('.avr').text());

        // Update stars to reflect the hover rating
        $('#agency-rating img.rating').each(function (index) {
            if (index < rating) {
                $(this).attr('src', '/img/FilledStar.jpeg');
            }else {
                $(this).attr('src', '/img/StarFade.gif');
            }
        });
        // Change the cursor to pointer when hovering over the star
        starImage.css("cursor", "pointer");

        // Initialize the tooltip (this should be called after setting the attributes)
        var tooltip = bootstrap.Tooltip.getInstance ? bootstrap.Tooltip.getInstance(starImage[0]) : null;

        if (tooltip) {
            tooltip.show();
        }
    }).on('mouseleave', 'img.rating', function () {
        var starImage = $(this);
        // Dispose of the tooltip only if it has been initialized
        var tooltip = bootstrap.Tooltip.getInstance ? bootstrap.Tooltip.getInstance(starImage[0]) : null;

        if (tooltip) {
            tooltip.dispose();
        }

        starImage.removeClass("toggle-password-visibility");
        // Reset stars to original state when mouse leaves
        // You can call the same function as above to reset the stars to the original rating state
        var av = parseFloat($('.avr').text());
        $('#agency-rating img.rating').each(function (index) {
            if (index < Math.floor(av)) {
                $(this).attr("src", "/img/FilledStar.jpeg");
            }else {
                $(this).attr("src", "/img/StarFade.gif");
            }
        });
    });

    $('#agency-rating').on('click', 'img.rating', function (e) {
        var starId = $(this).attr('id');
        var vendorId = $('#vendorId').val();

        console.log('Rated ' + starId + ' stars for vendor ' + vendorId);
        var url = "/Rating/PostRating?rating=" + starId + "&mid=" + vendorId;

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
                }else {
                    star.attr("src", "/img/StarFade.gif");  // Faded stars
                }
            });
        }
    }
});