$(document).ready(function () {
    $('#create-policy').on('click', function () {
        $("body").addClass("submit-progress-bg");
        // Wrap in setTimeout so the UI
        // can update the spinners
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);
        $('#create-policy').attr('disabled', 'disabled');
        $('#create-policy').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Add Policy");

        var article = document.getElementById("article");
        if (article) {
            var nodes = article.getElementsByTagName('*');
            for (var i = 0; i < nodes.length; i++) {
                nodes[i].disabled = true;
            }
        }
    });

    var askConfirmation = true;
    $('#create-form').submit(function (e) {
        if (askConfirmation) {
            e.preventDefault();
            $.confirm({
                title: "Confirm Add Policy",
                content: "Are you sure to add ?",
    
                icon: 'fas fa-thumbtack',
                closeIcon: true,
                type: 'red',
                buttons: {
                    confirm: {
                        text: "Add Policy",
                        btnClass: 'btn-danger',
                        action: function () {
                            askConfirmation = false;
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