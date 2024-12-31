$.validator.setDefaults({
    submitHandler: function (form) {
        const fileInput = $('#createImageInput')[0];
        if (!fileInput.files.length) {
            $.alert({
                title: "NO FILE SELECTED",
                content: "NO FILE SELECTED",
                icon: 'fas fa-exclamation-triangle',
                type: "red",
                closeIcon: true,
                buttons: {
                    cancel: {
                        text: "CLOSE",
                        btnClass: 'btn-danger'
                    }
                }
            });
        }
        else {
        $.confirm({
            title: "Confirm  Add Customer",
            content: "Are you sure to add ?",
            icon: 'fas fa-user-plus',

            type: 'green',
            closeIcon: true,
            buttons: {
                confirm: {
                    text: "Add Customer",
                    btnClass: 'btn-success',
                    action: function () {
                        $("body").addClass("submit-progress-bg");
                        // Wrap in setTimeout so the UI
                        // can update the spinners
                        setTimeout(function () {
                            $(".submit-progress").removeClass("hidden");
                        }, 1);
                        
                        $('#create-cust').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Add Customer");
                        disableAllInteractiveElements();
                        form.submit();
                        var createForm = document.getElementById("create-form");
                        if (createForm) {

                            var nodes = createForm.getElementsByTagName('*');
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

    }
});
$(document).ready(function () {
    $("#create-form").validate();
});

$("#customer-name").focus();

dateCustomerId.max = new Date().toISOString().split("T")[0];