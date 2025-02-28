$(document).ready(function () {
    //$('#RawMessage').summernote({
    //    height: 300,                 // set editor height
    //    minHeight: null,             // set minimum height of editor
    //    maxHeight: null,             // set maximum height of editor
    //    focus: false                  // set focus to editable area after initializing summernote
    //});
    var recepient = $("#receipient-email");
    var message = $("#Message");
    if (!recepient.val()) {
        recepient.focus();
    }
    else {
        message.focus();
    }
    var currentImage = document.getElementById('documentImage0').src;

    $("#document").on('change', function () {
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
                        if (currentImage.startsWith('https://') && currentImage.endsWith('/img/no-image.png')) {
                            document.getElementById('documentImage0').src = '/img/no-image.png';
                            document.getElementById('document').value = '';
                        }
                        $.alert(
                            {
                                title: " File UPLOAD issue !",
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
                        document.getElementById('documentImage0').src = window.URL.createObjectURL($(this)[0].files[i]);
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

    var askConfirmation = true;
    $('#email-form').submit(function (e) {
        if (askConfirmation) {
            e.preventDefault();
            $.confirm({
                title: "Confirm Send",

                content: "Are you sure to send?",
                icon: 'far fa-envelope',
                type: 'green',
                closeIcon: true,
                buttons: {
                    confirm: {
                        text: "SEND",
                        btnClass: 'btn-success',
                        action: function () {
                            askConfirmation = false;
                           
                            $('#send-email').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Send");
                            $('#email-form').submit();
                            disableAllInteractiveElements();
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

    let availableSuggestions = []; // Store available suggestions

    $("#receipient-email").autocomplete({
        source: function (request, response) {
            $("#loader").show(); // Show loader
            $.ajax({
                url: "/api/MasterData/GetUserBySearch",
                type: "GET",
                data: { search: request.term },
                success: function (data) {
                    availableSuggestions = data.map(item => ({
                        label: item,
                        value: item
                    })); // Update the available suggestions
                    response(availableSuggestions);
                    $("#loader").hide(); // Hide loader
                },
                error: function () {
                    availableSuggestions = []; // Clear suggestions on error
                    response([]);
                    $("#loader").hide(); // Hide loader
                }
            });
        },
        minLength: 1, // Start showing suggestions after 1 character
        select: function (event, ui) {
            // Set the selected value to the input field
            $("#receipient-email").val(ui.item.value);
        },
        messages: {
            noResults: "No results found",
            results: function (amount) {
                return `${amount} result${amount > 1 ? "s" : ""} found`;
            }
        }
    });

    $("#receipient-email").on("blur", function () {
        const inputValue = $(this).val().trim();

        // Check if the input value matches any available suggestion
        const isValid = availableSuggestions.some(item =>
            item.label === inputValue);

        // Reset the input field if invalid
        if (!isValid) {
            $(this).val("").addClass("input-invalid");
            $(this).focus();
        } else {
            $(this).removeClass("input-invalid");
        }
    });

});