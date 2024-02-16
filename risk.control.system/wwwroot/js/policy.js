$.validator.setDefaults({
    submitHandler: function (form) {
        $.confirm({
            title: "Confirm Add New",
            content: "Are you sure to add?",
            icon: 'far fa-file-powerpoint',
            columnClass: 'medium',
            type: 'green',
            closeIcon: true,
            buttons: {
                confirm: {
                    text: " Add New",
                    btnClass: 'btn-success',
                    action: function () {

                        $("body").addClass("submit-progress-bg");
                        // Wrap in setTimeout so the UI
                        // can update the spinners
                        setTimeout(function () {
                            $(".submit-progress").removeClass("hidden");
                        }, 1);
                        $('#create-policy').attr('disabled', 'disabled');
                        $('#create-policy').html("<i class='far fa-file-powerpoint' aria-hidden='true'></i> Add Policy .....");

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
$("#ContractNumber").focus();

dateContractId.max = new Date().toISOString().split("T")[0];
dateIncidentId.max = new Date().toISOString().split("T")[0];