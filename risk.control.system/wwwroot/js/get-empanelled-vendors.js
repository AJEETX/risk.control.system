$(function () {
    $("img.rating").mouseover(function () {
        giveRating($(this), "FilledStar.jpeg");
        $(this).css("cursor", "pointer");
    });

    $("img.rating").mouseout(function () {
        giveRating($(this), "EmptyStar.png");
        refilRating($(this));
    });

    $("img.rating").click(function (e) {
        // $("img.rating").unbind("mouseout mouseover click");
        $(this).css('color', 'red');
        // alert(e.currentTarget + ' was clicked!');
        // call ajax methods to update database
        var vendorId = $(this).attr("vendorId");
        var url = "/Vendors/PostRating?rating=" + parseInt($(this).attr("id")) + "&mid=" + $(this).attr("vendorId");
        $.post(url, null, function (data) {
            $(e.currentTarget).closest('tr').find('div.result').text(data).css('color', 'red');
            $("#result").text(data);
        });
    });

    $("#datatable > tbody  > tr").each(function () {
        var av = $(this).find("span.avr").text();

        if (av != "" || av != null) {
            // alert(av);
            // fillRating(parseInt(av));
            var img = $(this).find("img[id='" + parseInt(av) + "']");
            img.attr("src", "/images/FilledStar.jpeg").prevAll("img.rating").attr("src", "/images/FilledStar.jpeg");
        }
    });
});

function giveRating(img, image) {
    img.attr("src", "/Images/" + image)
        .prevAll("img.rating").attr("src", "/Images/" + image);
}
function refilRating(img1) {
    var rt = $(img1).closest('tr').find("span.avr").text();
    var img = $(img1).closest('tr').find("img[id='" + parseInt(rt) + "']");
    img.attr("src", "/images/FilledStar.jpeg").prevAll("img.rating").attr("src", "/images/FilledStar.jpeg");
}