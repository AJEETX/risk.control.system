$(function () {
    $("img.main-rating").mouseover(function () {
        giveRating($(this), "FilledStar.jpeg");
        $(this).css("cursor", "pointer");
    });

    $("img.main-rating").mouseout(function () {
        giveRating($(this), "StarFade.gif");
        refilRating($(this));
    });

    $("img.main-rating").click(function (e) {
        $(this).css('color', 'red');
        var url = "/Vendors/PostRating?rating=" + parseInt($(this).attr("id")) + "&mid=" + $(this).attr("vendorId");
        $.post(url, null, function (data) {
            $("#rating-result-data").text(data).css('color', 'red');
        });
    });

    $("#agency-rating").each(function () {
        var av = $(this).find("span.avr").text();

        if (av != "" || av != null) {
            var img = $(this).find("img[id='" + parseInt(av) + "']");
            img.attr("src", "/images/FilledStar.jpeg").prevAll("img.main-rating").attr("src", "/images/FilledStar.jpeg");
        }
    });
});

function giveRating(img, image) {
    img.attr("src", "/Images/" + image)
        .prevAll("img.main-rating").attr("src", "/Images/" + image);
}
function refilRating(img1) {
    var rt = $(img1).closest('#agency-rating').find("span.avr").text();
    var img = $(img1).closest('#agency-rating').find("img[id='" + parseInt(rt) + "']");
    img.attr("src", "/images/FilledStar.jpeg").prevAll("img.main-rating").attr("src", "/images/FilledStar.jpeg");
}