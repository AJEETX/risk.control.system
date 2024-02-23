$.validator.setDefaults({
    submitHandler: function (form) {
        $.confirm({
            title: "Confirm  Add Customer",
            content: "Are you sure to add ?",
            icon: 'fas fa-user-plus',
            columnClass: 'medium',
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
                        $('#create-cust').attr('disabled', 'disabled');
                        $('#create-cust').html("<i class='fas fa-spinner' aria-hidden='true'></i> Add Customer");

                        form.submit();
                        var nodes = document.getElementById("article").getElementsByTagName('*');
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