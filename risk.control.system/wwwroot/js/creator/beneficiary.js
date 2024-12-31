$(document).ready(function () {

    var askConfirmation = true;

    $('#create-form').submit(function (e) {
        // Validate the form before showing the confirmation prompt
        if ($("#create-form").valid() && askConfirmation) {
            e.preventDefault(); // Prevent form submission

            $.confirm({
                title: "Confirm Add Beneficiary",
                content: "Are you sure to add?",
                icon: 'fas fa-user-tie',

                type: 'green',
                closeIcon: true,
                buttons: {
                    confirm: {
                        text: "Add Beneficiary",
                        btnClass: 'btn-success',
                        action: function () {
                            askConfirmation = false;
                            $("body").addClass("submit-progress-bg");
                            // Wrap in setTimeout so the UI
                            // can update the spinners
                            setTimeout(function () {
                                $(".submit-progress").removeClass("hidden");
                            }, 1);

                            $('#create-bene').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Add Beneficiary");
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
        } else if(askConfirmation) {
            // If the form is not valid, prevent form submission and show a validation error
            e.preventDefault();
            $.alert({
                title: "Form Validation Error",
                content: "Please fill in all required fields correctly.",
                icon: 'fas fa-exclamation-triangle',
                type: 'red',
                closeIcon: true,
                buttons: {
                    ok: {
                        text: "OK",
                        btnClass: 'btn-danger'
                    }
                }
            });
        }
    });

    // Initialize the form validation
    $("#create-form").validate();

    var askEditConfirmation = true;

    $('#edit-form').submit(function (e) {
        // Validate the form before showing the confirmation prompt
        if ($("#edit-form").valid() && askEditConfirmation) {
            e.preventDefault(); // Prevent form submission

            $.confirm({
                title: "Confirm  Edit Beneficiary",
                content: "Are you sure to edit?",
                icon: 'fas fa-user-tie',

                type: 'orange',
                closeIcon: true,
                buttons: {
                    confirm: {
                        text: "Edit Beneficiary",
                        btnClass: 'btn-warning',
                        action: function () {
                            askEditConfirmation = false;
                            $("body").addClass("submit-progress-bg");
                            // Wrap in setTimeout so the UI
                            // can update the spinners
                            setTimeout(function () {
                                $(".submit-progress").removeClass("hidden");
                            }, 1);

                            $('#create-bene').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Edit Beneficiary");
                            disableAllInteractiveElements();

                            $('#edit-form').submit();
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
        } else if (askEditConfirmation){
            // If the form is not valid, prevent form submission and show a validation error
            e.preventDefault();
            $.alert({
                title: "Form Validation Error",
                content: "Please fill in all required fields correctly.",
                icon: 'fas fa-exclamation-triangle',
                type: 'red',
                closeIcon: true,
                buttons: {
                    ok: {
                        text: "OK",
                        btnClass: 'btn-danger'
                    }
                }
            });
        }
    });

    $("#edit-form").validate();

});

$("#BeneficiaryName").focus();
