$(function () {
    $("img.rating").mouseover(function () {
        giveRating($(this), "FilledStar.jpeg");
        $(this).css("cursor", "pointer");
    });

    $("img.rating").mouseout(function () {
        giveRating($(this), "StarFade.gif");
        refilRating($(this));
    });

    $("img.rating").click(function (e) {
        $(this).css('color', 'red');
        var url = "/Vendors/PostRating?rating=" + parseInt($(this).attr("id")) + "&mid=" + $(this).attr("vendorId");
        $.post(url, null, function (data) {
            $(e.currentTarget).closest('tr').find('span.result').text(data).css('color', 'red');
            $("#result").text(data);
        });
    });

    $("#datatable > tbody  > tr").each(function () {
        var av = $(this).find("span.avr").text();

        if (av != "" || av != null) {
            var img = $(this).find("img[id='" + parseInt(av) + "']");
            img.attr("src", "/images/FilledStar.jpeg").prevAll("img.rating").attr("src", "/images/FilledStar.jpeg");
        }
    });
    var askConfirmation = true;
    $('#radioButtons').submit(function (e) {
        if (askConfirmation) {
            e.preventDefault();
            $.confirm({
                title: "Confirm Allocation",
                content: "Are you sure to allocate?",
                columnClass: 'medium',
                icon: 'fas fa-external-link-alt',
                type: 'red',
                closeIcon: true,
                buttons: {
                    confirm: {
                        text: "Allocate",
                        btnClass: 'btn-danger',
                        action: function () {
                            askConfirmation = false;
                            $('#radioButtons').submit();
                        }
                    },
                    cancel: {
                        text: "Cancel",
                        btnClass: 'btn-default'
                    }
                }
            });
        }
    })
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