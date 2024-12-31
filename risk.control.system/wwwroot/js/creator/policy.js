$.validator.setDefaults({
    submitHandler: function (form) {
        $.confirm({
            title: "Confirm Add Policy",
            content: "Are you sure to add?",
            icon: 'far fa-file-powerpoint',

            type: 'green',
            closeIcon: true,
            buttons: {
                confirm: {
                    text: " Add Policy",
                    btnClass: 'btn-success',
                    action: function () {
                        $("body").addClass("submit-progress-bg");
                        // Wrap in setTimeout so the UI
                        // can update the spinners
                        setTimeout(function () {
                            $(".submit-progress").removeClass("hidden");
                        }, 1);
                        
                        $('#create-policy').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Add Policy");
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

    $("#ContractNumber").focus();

});

var dateContractId = document.getElementById("dateContractId");
if (dateContractId) {
    dateContractId.max = new Date().toISOString().split("T")[0];
}

var dateIncidentId = document.getElementById("dateIncidentId");
if (dateIncidentId) {
    dateIncidentId.max = new Date().toISOString().split("T")[0];
}

//dateContractId.max = new Date().toISOString().split("T")[0];
//dateIncidentId.max = new Date().toISOString().split("T")[0];
