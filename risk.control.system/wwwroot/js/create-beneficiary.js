$("#BeneficiaryName").focus();
BeneficiaryDateOfBirthId.max = new Date().toISOString().split("T")[0];

$.validator.setDefaults({
    submitHandler: function (form) {
        $.confirm({
            title: "Confirm Add New",
            content: "Are you sure to add?",
            icon: 'fas fa-user-tie',
            columnClass: 'medium',
            type: 'green',
            closeIcon: true,
            buttons: {
                confirm: {
                    text: "Add New",
                    btnClass: 'btn-success',
                    action: function () {
                        $("body").addClass("submit-progress-bg");
                        // Wrap in setTimeout so the UI
                        // can update the spinners
                        setTimeout(function () {
                            $(".submit-progress").removeClass("hidden");
                        }, 1);
                        $('#create-bene').attr('disabled', 'disabled');
                        $('#create-bene').html("<i class='fas fa-user-tie' aria-hidden='true'></i> Add Beneficiary...");

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