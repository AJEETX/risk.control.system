﻿$("#BeneficiaryName").focus();
BeneficiaryDateOfBirthId.max = new Date().toISOString().split("T")[0];

$.validator.setDefaults({
    submitHandler: function (form) {
        $.confirm({
            title: "Confirm  Edit Beneficiary",
            content: "Are you sure to edit?",
            icon: 'fas fa-user-tie',
            columnClass: 'medium',
            type: 'orange',
            closeIcon: true,
            buttons: {
                confirm: {
                    text: "Edit Beneficiary",
                    btnClass: 'btn-warning',
                    action: function () {
                        $("body").addClass("submit-progress-bg");
                        // Wrap in setTimeout so the UI
                        // can update the spinners
                        setTimeout(function () {
                            $(".submit-progress").removeClass("hidden");
                        }, 1);
                        $('#create-bene').attr('disabled', 'disabled');
                        $('#create-bene').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Edit Beneficiary");

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