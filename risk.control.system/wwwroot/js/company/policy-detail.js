$(document).ready(function () {

    var askConfirmation = true;
    $('#create-form').on('submit', function (e) {
        if (askConfirmation) {
            e.preventDefault();
            $.confirm({
                title: "Confirm Assign<span class='badge badge-light'>(auto)</span>",
                content: "Are you sure to Assign<span class='badge badge-light'>(auto)</span> ?",
                icon: 'fas fa-random',
                type: 'orange',
                closeIcon: true,
                buttons: {
                    confirm: {
                        text: "Assign <span class='badge badge-warning'>(auto)</span>",
                        btnClass: 'btn-warning',
                        action: function () {
                            askConfirmation = false;

                            $("body").addClass("submit-progress-bg");
                            // Wrap in setTimeout so the UI
                            // can update the spinners
                            setTimeout(function () {
                                $(".submit-progress").removeClass("hidden");
                            }, 1);
                            
                            $('#assign-list').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Assign<span class='badge badge-light'>(auto)</span>");
                            disableAllInteractiveElements();

                            $('#create-form').submit();
                            var article = document.getElementById("article");
                            if (article) {
                                var nodes = article.getElementsByTagName('*');
                                for (var i = 0; i < nodes.length; i++) {
                                    nodes[i].disabled = true;
                                }
                            }
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

    $('#edit-policy').on('click', function () {
        $("body").addClass("submit-progress-bg");
        // Wrap in setTimeout so the UI
        // can update the spinners
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);
        
        $('#edit-policy').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Edit  Case");
        disableAllInteractiveElements();

        var article = document.getElementById("article");
        if (article) {
            var nodes = article.getElementsByTagName('*');
            for (var i = 0; i < nodes.length; i++) {
                nodes[i].disabled = true;
            }
        }
    });

    $('a#add-customer').on('click', function () {
        $("body").addClass("submit-progress-bg");
        // Wrap in setTimeout so the UI
        // can update the spinners
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);
        
        $('#add-customer').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Add Customer");
        disableAllInteractiveElements();

        var article = document.getElementById("article");
        if (article) {
            var nodes = article.getElementsByTagName('*');
            for (var i = 0; i < nodes.length; i++) {
                nodes[i].disabled = true;
            }
        }
    });

    $('#edit-customer').on('click', function () {
        $("body").addClass("submit-progress-bg");
        // Wrap in setTimeout so the UI
        // can update the spinners
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);
        
        $('#edit-customer').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Edit Customer");
        disableAllInteractiveElements();

        var article = document.getElementById("article");
        if (article) {
            var nodes = article.getElementsByTagName('*');
            for (var i = 0; i < nodes.length; i++) {
                nodes[i].disabled = true;
            }
        }
    });

    $('#add-beneficiary').on('click', function () {
        $("body").addClass("submit-progress-bg");
        // Wrap in setTimeout so the UI
        // can update the spinners
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);
        
        $('#add-beneficiary').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Add Beneficiary");
        disableAllInteractiveElements();

        var article = document.getElementById("article");
        if (article) {
            var nodes = article.getElementsByTagName('*');
            for (var i = 0; i < nodes.length; i++) {
                nodes[i].disabled = true;
            }
        }
    });

    $('#edit-beneficiary').on('click', function () {
        $("body").addClass("submit-progress-bg");
        // Wrap in setTimeout so the UI
        // can update the spinners
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);
        
        $('#edit-beneficiary').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Edit Beneficiary");
        disableAllInteractiveElements();

        var article = document.getElementById("article");
        if (article) {
            var nodes = article.getElementsByTagName('*');
            for (var i = 0; i < nodes.length; i++) {
                nodes[i].disabled = true;
            }
        }
    });

    $('#assign-manual-list').on('click', function () {
        $("body").addClass("submit-progress-bg");
        // Wrap in setTimeout so the UI
        // can update the spinners
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);
        
        $('#assign-manual-list').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Assign<b> <sub>manual</sub></b>");
        disableAllInteractiveElements();

        var article = document.getElementById("article");
        if (article) {
            var nodes = article.getElementsByTagName('*');
            for (var i = 0; i < nodes.length; i++) {
                nodes[i].disabled = true;
            }
        }
    });

    $('#remarks').on('keydown', function () {
        var report = $('#remarks').val();
        if (report != '') {
            //Enable the submit button.
            $('#submit-case').attr("disabled", false);
        } else {
            //If it is not checked, disable the button.
            $('#submit-case').attr("disabled", true);
        }
    })

    $('#remarks').on('blur', function () {
        var report = $('#remarks').val();
        if (report != '') {
            //Enable the submit button.
            $('#submit-case').attr("disabled", false);
        } else {
            //If it is not checked, disable the button.
            $('#submit-case').attr("disabled", true);
        }
    })

    $('#withdraw-information-popup').on('click', function (e) {
        $.alert(
            {
                title: " Withdraw Claim !",
                content: "The case can not be withdrawn. See <i class='fas fa-clock'></i> Timeline section  for more info",
                icon: 'fas fa-info',
                animationBounce: 2.5,
                type: 'red',
                closeIcon: true,
                buttons: {
                    cancel: {
                        text: "CLOSE",
                        btnClass: 'btn-danger'
                    }
                }
            }
        );
    });
    var withdrawAskConfirmation = true;
    $('#withdraw-form').submit(function (e) {
        if (withdrawAskConfirmation) {
            e.preventDefault();
            $.confirm({
                title: "Confirm withdrawal",
                content: "Are you sure?",

                icon: 'fas fa-undo',
                closeIcon: true,
                type: 'red',
                buttons: {
                    confirm: {
                        text: "Withdraw case",
                        btnClass: 'btn-danger',
                        action: function () {
                            withdrawAskConfirmation = false;

                            $("body").addClass("submit-progress-bg");
                            // Wrap in setTimeout so the UI
                            // can update the spinners
                            setTimeout(function () {
                                $(".submit-progress").removeClass("hidden");
                            }, 1);
                            
                            $('#submit-case').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Withdraw");
                            disableAllInteractiveElements();

                            $('#withdraw-form').submit();
                            var article = document.getElementById("article");
                            if (article) {
                                var nodes = article.getElementsByTagName('*');
                                for (var i = 0; i < nodes.length; i++) {
                                    nodes[i].disabled = true;
                                }
                            }
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