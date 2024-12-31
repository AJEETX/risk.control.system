$.validator.setDefaults({
    submitHandler: function (form) {
        $.confirm({
            title: "Confirm Edit Policy",
            content: "Are you sure to edit?",

            icon: 'far fa-file-powerpoint',
            type: 'orange',
            closeIcon: true,
            buttons: {
                confirm: {
                    text: "Edit Policy",
                    btnClass: 'btn-warning',
                    action: function () {
                        $("body").addClass("submit-progress-bg");
                        // Wrap in setTimeout so the UI
                        // can update the spinners
                        setTimeout(function () {
                            $(".submit-progress").removeClass("hidden");
                        }, 1);

                        $('#create-policy').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Edit Policy");
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
});
$(document).ready(function () {
    $("#create-form").validate();
});
dateContractId.max = new Date().toISOString().split("T")[0];
dateIncidentId.max = new Date().toISOString().split("T")[0];