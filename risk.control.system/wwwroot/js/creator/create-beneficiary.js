$("#BeneficiaryName").focus();

var BeneficiaryDateOfBirthId = document.getElementById("BeneficiaryDateOfBirthId");
if (BeneficiaryDateOfBirthId) {
    BeneficiaryDateOfBirthId.max = new Date().toISOString().split("T")[0];
}

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
                            $("body").addClass("submit-progress-bg");
                            // Wrap in setTimeout so the UI
                            // can update the spinners
                            setTimeout(function () {
                                $(".submit-progress").removeClass("hidden");
                            }, 1);

                            $('#create-bene').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Add Beneficiary");
                            disableAllInteractiveElements();
                            form.submit();
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
        
    }
});
$(document).ready(function () {
    $("#create-form").validate();
});