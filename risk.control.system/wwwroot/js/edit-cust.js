$.validator.setDefaults({
    submitHandler: function (form) {
        $.confirm({
            title: "Confirm Edit Customer",
            content: "Are you sure to edit?",
            icon: 'fas fa-user-plus',
            type: 'orange',
            columnClass: 'medium',
            closeIcon: true,
            buttons: {
                confirm: {
                    text: "Edit Customer",
                    btnClass: 'btn-orange',
                    action: function () {
                        $("body").addClass("submit-progress-bg");
                        // Wrap in setTimeout so the UI
                        // can update the spinners
                        setTimeout(function () {
                            $(".submit-progress").removeClass("hidden");
                        }, 1);
                        $('#create-cust').attr('disabled', 'disabled');
                        $('#create-cust').html("<i class='fas fa-spinner' aria-hidden='true'></i> Edit Customer");

                        form.submit();
                        var nodes = document.getElementById("create-form").getElementsByTagName('*');
                        for (var i = 0; i < nodes.length; i++) {
                            nodes[i].disabled = true;
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
});
$(document).ready(function () {
    $("#create-form").validate();
});
$("#customer-name").focus();

dateCustomerId.max = new Date().toISOString().split("T")[0];