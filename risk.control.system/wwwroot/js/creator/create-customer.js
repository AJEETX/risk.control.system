$.validator.setDefaults({
    submitHandler: function (form) {
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
                        $('#create-cust').attr('disabled', 'disabled');
                        $('body').attr('disabled', 'disabled');
                        $('html *').css('cursor', 'not-allowed');
                        $('button').prop('disabled', true);
                        $('a.btn *').removeAttr('href');
                        $('html a *, html button *').css('pointer-events', 'none');
                        $('#create-cust').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Add Customer");

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
        var preloadedPinCodeId = $("#SelectedId").val(); // Get the hidden field value

        // Fetch and populate PinCodeName if PinCodeId exists
        if (preloadedPinCodeId) {
            $.ajax({
                url: '/api/Company/GetPincodeName', // Endpoint to fetch PinCodeName
                type: 'GET',
                data: { pincodeId: preloadedPinCodeId },
                success: function (response) {
                    if (response && response.pincodeName) {
                        $("#PinCodeId").val(response.pincodeName); // Populate input with name
                    }
                },
                error: function () {
                    console.error('Failed to fetch PinCodeName');
                }
            });
        }

        // Autocomplete logic remains the same
        $("#PinCodeId").autocomplete({
            source: function (request, response) {
            $.ajax({
                url: '/api/Company/Search',
                data: {
                    term: request.term,
                    districtId: $("#DistrictId").val(),
                    stateId: $("#StateId").val(),
                    countryId: $("#CountryId").val()
                },
                success: function (data) {
                    response($.map(data, function (item) {
                        return {
                            label: item.pincodeName,
                            value: item.pincodeName,
                            id: item.pincodeId
                        };
                    }));
                }
            });
            },
        minLength: 1,
        select: function (event, ui) {
            $("#PinCodeId").val(ui.item.label); // Set name in input
            $("#SelectedId").val(ui.item.id);  // Set ID in hidden field
            return false;
            }
        });

        // Clear the hidden field if the input is cleared manually
        $("#PinCodeId").on('input', function () {
            if (!$(this).val()) {
                $("#SelectedId").val('');
            }
        });
    $('#PinCodeId').on('focus', function () {
        $(this).select();
    });
    $("#create-form").validate();
    $("#documentImageInput").on('change', function () {
        var MaxSizeInBytes = 2097152;
        //Get count of selected files
        var countFiles = $(this)[0].files.length;

        var imgPath = $(this)[0].value;
        var extn = imgPath.substring(imgPath.lastIndexOf('.') + 1).toLowerCase();

        if (extn == "gif" || extn == "png" || extn == "jpg" || extn == "jpeg") {
            if (typeof (FileReader) != "undefined") {

                //loop for each file selected for uploaded.
                for (var i = 0; i < countFiles; i++) {
                    var fileSize = $(this)[0].files[i].size;
                    if (fileSize > MaxSizeInBytes) {
                        document.getElementById('profileImage').src = '/img/no-user.png';
                        document.getElementById('documentImageInput').value = '';
                        $.alert(
                            {
                                title: " Image UPLOAD issue !",
                                content: " <i class='fa fa-upload'></i> Upload Image size limit exceeded. <br />Max file size is 2 MB!",
                                icon: 'fas fa-exclamation-triangle',
                                type: 'red',
                                closeIcon: true,
                                buttons: {
                                    cancel: {
                                        text: "CLOSE",
                                        btnClass: 'btn-danger'
                                    }
                                }
                            }
                        );
                    }
                    else {
                        document.getElementById('profileImage').src = window.URL.createObjectURL($(this)[0].files[i]);
                    }
                }

            } else {
                $.alert(
                    {
                        title: "Outdated Browser !",
                        content: "This browser does not support FileReader. Try on modern browser!",
                        icon: 'fas fa-exclamation-triangle',
            
                        type: 'red',
                        closeIcon: true,
                        buttons: {
                            cancel: {
                                text: "CLOSE",
                                btnClass: 'btn-danger'
                            }
                        }
                    }
                );
            }
        } else {
            $.alert(
                {
                    title: "FILE UPLOAD TYPE !!",
                    content: "Pls select only image with extension jpg, png,gif ! ",
                    icon: 'fas fa-exclamation-triangle',
        
                    type: 'red',
                    closeIcon: true,
                    buttons: {
                        cancel: {
                            text: "CLOSE",
                            btnClass: 'btn-danger'
                        }
                    }
                }
            );
        }
    });
});

$("#customer-name").focus();

dateCustomerId.max = new Date().toISOString().split("T")[0];