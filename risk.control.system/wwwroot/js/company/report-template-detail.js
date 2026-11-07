$(document).ready(function () {

    // Generic handler for Face, Document, and Media toggle checkboxes
    $(document).on('change', '.doc-toggle-checkbox, .face-toggle-checkbox, .media-toggle-checkbox', function () {
        var isChecked = $(this).is(':checked');
        var targetId = $(this).data('target');
        var $container = $('#' + targetId);

        // Enable or disable all dependent inputs inside the container
        $container.find('.dependent-input').prop('disabled', !isChecked);

        // If unchecked, clear/reset the dependent input values
        if (!isChecked) {
            $container.find('input[type="checkbox"].dependent-input').prop('checked', false);
            $container.find('input[type="hidden"].dependent-input').val('');
        }
    });

    // Add Question button click event
    $(document).on('click', '.add-question-btn', function () {
        var locationId = $(this).data('locationid');

        // Reset form and set location
        $('#questionAddForm')[0].reset();
        $('#optionsInput').prop('required', false); // prevent validation error

        $('#addQuestionModal').find('input[name="LocationId"]').val(locationId);

        // Handle options visibility based on the selected type (default state)
        var questionType = $('#QuestionType').val(); // ✅ get the value, not the element
        if (questionType === 'dropdown' || questionType === 'radiobutton' || questionType === 'checkbox') {
            $('#optionsContainer').removeClass('d-none').show();
        } else {
            $('#optionsContainer').addClass('d-none').hide();
        }

        $('#addQuestionModal')
            .off('shown.bs.modal') // 🟢 correct event name
            .on('shown.bs.modal', function () {
                $('#QuestionText').trigger('focus');
            })
            .modal('show'); // show after binding
    });

    // toggle options container when type changes (optional)
    $(document).on('change', '#QuestionType', function () {
        const t = $(this).val();
        const $optionsContainer = $('#optionsContainer');
        const $optionsInput = $('#optionsInput');

        if (t === 'dropdown' || t === 'radiobutton' || t === 'checkbox') {
            $optionsContainer.removeClass('d-none').show();
            $optionsInput.prop('required', true); // enable required
        } else {
            $optionsContainer.addClass('d-none').hide();
            $optionsInput.prop('required', false); // disable required
        }
    });

    // Handle Question Add form submission
    $(document).on('submit', '#questionAddForm', function (e) {
        e.preventDefault();

        // read + normalize inputs
        var locationIdRaw = $('#questionAddForm input[name="LocationId"]').val();
        var locationId = sanitizeId(locationIdRaw);

        var optionsInput = $('#optionsInput').val() || "";
        var newQuestionText = $('#QuestionText').val() || "";
        var newQuestionType = $('#QuestionType').val() || "";

        // fix: explicit boolean read
        var isRequired = !!$('#isRequired').is(':checked');

        // optional: basic client-side validation
        if (!newQuestionText.trim()) {
            $.alert({ title: 'Validation', content: 'Question text cannot be empty.', type: 'orange' });
            return;
        }

        $.ajax({
            url: '/ReportTemplate/AddQuestion',
            method: 'POST',
            data: {
                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val(),
                locationId: locationId,                 // sanitized
                optionsInput: optionsInput,
                newQuestionText: newQuestionText,
                newQuestionType: newQuestionType,
                isRequired: isRequired
            },
            success: function (response) {
                if (response.success) {
                    $('#addQuestionModal').modal('hide');

                    $.confirm({
                        title: '<span class="i-green"> <i class="fas fa-question"></i> </span> Question',
                        content: 'Question has been added successfully.',
                        type: 'green',
                        buttons: { ok: function () { /* no-op */ } }
                    });

                    var q = response.updatedQuestion;
                    if (q && locationId) {
                        // Sanitize remote question properties
                        var qText = safeText(q.questionText);
                        var qType = safeText(q.questionType);
                        var qOptions = String(q.options || "");
                        var qIsRequired = !!q.isRequired; // coerce
                        var qId = sanitizeId(q.id);       // sanitize id before using as data attr

                        // Create list item
                        var $li = $("<li>").addClass("mb-2");

                        var $container = $("<div>").addClass("border rounded p-2 bg-light");
                        var $row = $("<div>").addClass("row");

                        // Left column
                        var $colLeft = $("<div>").addClass("col-md-11");

                        var $spanText = $("<span>").text(qText);
                        $colLeft.append($spanText);

                        // Required asterisk
                        if (qIsRequired) {
                            var $required = $("<span>")
                                .addClass("required-asterisk text-danger fw-bold")
                                .text("*")
                                .attr("data-bs-toggle", "tooltip")
                                .attr("data-bs-placement", "top")
                                .attr("title", "Required field");

                            $colLeft.append(" ").append($required);
                        }

                        // Question type
                        if (qType) {
                            var $smallType = $("<small>")
                                .addClass("text-muted")
                                .text("[" + qType + "]");
                            $colLeft.append(document.createTextNode(" ")).append($smallType);
                        }

                        // Options (non-text questions)
                        if (qType && qType.toLowerCase() !== "text" && qOptions) {
                            var $optionsDiv = $("<div>").addClass("mt-4");

                            // Parse options, sanitize each option text
                            var optionList = qOptions.split(",")
                                .map(function (o) { return safeText(o.trim()); })
                                .filter(function (o) { return o.length > 0; });

                            optionList.forEach(function (optText) {
                                var $opt = $("<span>")
                                    .addClass("badge bg-light text-dark border me-1")
                                    .text(optText);
                                $optionsDiv.append($opt);
                            });

                            $colLeft.append($optionsDiv);
                        }

                        // Right column: Delete button
                        var $colRight = $("<div>").addClass("mt-2 text-end"); // align right

                        var $deleteBtn = $("<button>")
                            .addClass("btn btn-sm btn-outline-danger delete-question-btn")
                            .attr("data-questionid", qId)        // sanitized
                            .attr("data-locationid", locationId); // sanitized

                        var $icon = $("<i>").addClass("fas fa-trash me-1");
                        var $small = $("<small>").text(" Delete");

                        $deleteBtn.append($icon).append($small);
                        $colRight.append($deleteBtn);

                        // assemble
                        $row.append($colLeft).append($colRight);
                        $container.append($row);
                        $li.append($container);

                        // Directly find the <ul> that belongs to this specific location ID
                        var $list = $('button.add-question-btn[data-locationid="' + locationId + '"]')
                            .closest('.card')
                            .find('ul.list-unstyled')
                            .first();

                            if ($list.length) {
                                $list.append($li);
                            }
                        }
                } else {
                    $.alert({ title: 'Error', content: response.message || 'Failed to add question.', type: 'red' });
                }
            },
            error: function () {
                $.alert({ title: 'Error', content: 'An error occurred while adding the question.', type: 'red' });
            }
        });
    });

    //Delete question
    $(document).on("click", ".delete-question-btn", function (e) {
        e.preventDefault();

        var $btn = $(this);
        var questionId = $(this).data("questionid");
        var locationId = $(this).data("locationid");
        var $row = $(this).closest("li"); // question row

        if (!questionId || !locationId) {
            $.alert({
                title: "Error",
                content: "Missing question ID or location ID.",
                type: "red"
            });
            return;
        }
        var $spinner = $(".submit-progress"); // global spinner (you already have this)

        $.confirm({
            title: 'Confirm Delete',
            icon: 'fas fa-trash',
            content: 'Are you sure you want to delete this question?',
            type: 'red',
            buttons: {
                confirm: {
                    text: 'Yes, delete it',
                    btnClass: 'btn-red',
                    action: function () {
                        $spinner.removeClass("hidden");
                        $btn.prop("disabled", true).html('<i class="fas fa-sync fa-spin"></i> Delete');
                        $.ajax({
                            url: '/ReportTemplate/DeleteQuestion',
                            type: 'POST',
                            data: {
                                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val(),
                                id: questionId,
                                locationId: locationId
                            },
                            success: function (response) {
                                if (response.success) {
                                    $row.remove(); // remove from UI
                                    $.alert({
                                        title: '<span class="i-orangered"> <i class="fas fa-trash"></i> </span> Deleted',
                                        content: 'Question has been deleted successfully!',
                                        type: 'red'
                                    });
                                } else {
                                    $.alert({
                                        title: '<span class="i-orangered"> <i class="fas fa-exclamation-triangle"></i> </span> Error!',
                                        content: response.message || 'Failed to delete question.',
                                        type: 'red'
                                    });
                                }
                            },
                            error: function () {
                                $.alert({
                                    title: '<span class="i-orangered"> <i class="fas fa-exclamation-triangle"></i> </span> Error!',
                                    content: 'An error occurred while deleting.',
                                    type: 'red'
                                });
                            },
                            complete: function () {
                                $spinner.addClass("hidden");
                                // ✅ Re-enable button and restore text
                                $btn.prop("disabled", false).html('<i class="fas fa-trash"></i> Delete');
                            }
                        });
                    }
                },
                cancel: function () {
                    // Do nothing
                }
            }
        });
    });

    //Delete location
    $(document).on("click", ".delete-location-btn", function (e) {
        e.preventDefault();

        var $btn = $(this);
        var locationDeletable = $('#locationCount').val() > 1;
        if (!locationDeletable) {
            $.alert({
                title: '<span class="i-orangered"> <i class="fas fa-exclamation-triangle"></i> </span> Error!',
                content: 'Single Location not DELETED.',
                type: 'red'
            });
        }
        else {
            var $spinner = $(".submit-progress"); // global spinner (you already have this)

            var locationId = $(this).data("locationid");

            if (!locationId) {
                $.alert({
                    title: "Error",
                    content: "Missing location ID.",
                    type: "red"
                });
                return;
            }
            var $card = $(this).closest(".col-12"); // outer card for that location

            $.confirm({
                title: 'Confirm Delete',
                icon: 'fas fa-trash',
                content: 'Are you sure you want to delete this location?',
                type: 'red',
                buttons: {
                    confirm: {
                        text: 'Yes, delete it',
                        btnClass: 'btn-red',
                        action: function () {
                            $spinner.removeClass("hidden");
                            $btn.prop("disabled", true).html('<i class="fas fa-sync fa-spin"></i> Delete');
                            $.ajax({
                                url: '/ReportTemplate/DeleteLocation',
                                type: 'POST',
                                data: {
                                    __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val(),
                                    id: locationId,
                                    locationDeletable: $('#locationCount').val() > 1
                                },
                                success: function (response) {
                                    if (response.success) {
                                        $card.remove(); // remove location from UI
                                        var existingCount = $('#locationCount').val();
                                        $('#locationCount').val(existingCount - 1);
                                        $.alert({
                                            title: '<span class="i-red"> <i class="fas fa-trash"></i> </span> Deleted!',
                                            content: 'Location deleted successfully!',
                                            type: 'red'
                                        });
                                    } else {
                                        $.alert({
                                            title: '<span class="i-orangered"> <i class="fas fa-exclamation-triangle"></i> </span> Error!',
                                            content: response.message || 'Failed to delete location.',
                                            type: 'red'
                                        });
                                    }
                                },
                                error: function () {
                                    $.alert({
                                        title: '<span class="i-orangered"> <i class="fas fa-exclamation-triangle"></i> </span> Error!',
                                        content: 'An error occurred while deleting.',
                                        type: 'red'
                                    });
                                },
                                complete: function () {
                                    $spinner.addClass("hidden");
                                    // ✅ Re-enable button and restore text
                                    $btn.prop("disabled", false).html('<i class="fas fa-trash"></i> Delete');
                                }
                            });
                        }
                    },
                    cancel: function () {
                        // user canceled
                    }
                }
            });
        }
    });

    //Save locations' FaceId, DocumentIds, ...
   $(document).on("click", ".save-location-btn", function (e) {
    e.preventDefault();

    var $btn = $(this);
    var locationId = $btn.data("locationid");
    var templateId = $btn.data("reporttemplateid");
    var $card = $btn.closest(".card"); 
    var $spinner = $(".submit-progress"); 

    // Get Location Name
    var locationName = $card.find("input.form-control.title-name").val()
        || $card.find("input[asp-for$='LocationName']").val()
        || $card.find("input[id^='location_']").val();

    if (!locationName || !locationName.trim()) {
        $.alert({
            title: '<span class="i-orangered"><i class="fas fa-exclamation-triangle"></i></span> Error!',
            content: "Location name is empty.",
            type: "red"
        });
        return;
    }

    // Helper function to read camera capture mode safely
       function getCameraCaptureMode($container) {
           var $checkedRadio = $container.find("input[type='radio']:checked");
           if ($checkedRadio.length > 0) {
               return $checkedRadio.val();
           }
           var $hiddenInput = $container.find("input[type='hidden'][name='CameraCaptureMode']");
           return $hiddenInput.length > $hiddenInput.val() ? $hiddenInput.val() : "Front";
       }

    // Collect FaceIds
       var faceIds = [];
       $card.find("input[id^='face_']").each(function () {
           var faceId = $(this).attr("id").replace("face_", "");
           var $container = $(this).closest(".border");
           var isRequired = $card.find("#isfaceRequiredCheckbox_" + faceId).is(":checked");

           faceIds.push({
               Id: faceId,
               Selected: $(this).is(":checked"),
               Required: isRequired,
               CameraCaptureMode: getCameraCaptureMode($container)
           });
       });

       // Collect DocumentIds
       var documentIds = [];
       $card.find("input[id^='doc_']").each(function () {
           var docId = $(this).attr("id").replace("doc_", "");
           var $container = $(this).closest(".border");
           var isRequired = $card.find("#isdocRequiredCheckbox_" + docId).is(":checked");

           // Get the selected PageCount enum value (e.g., "SINGLE", "DOUBLE", or "MULTIPLE")
           var pageCount = $container.find("input[id^='pageCount_" + docId + "_']:checked").val();

           documentIds.push({
               Id: docId,
               Selected: $(this).is(":checked"),
               Required: isRequired,
               PageCount: pageCount, // Added PageCount value here
               CameraCaptureMode: 1
           });
       });
    // Collect MediaReports
       var mediaReports = [];
       $card.find("input[id^='media_']").each(function () {
           var mediaId = $(this).attr("id").replace("media_", "");
           var $container = $(this).closest(".border");
           var isRequired = $card.find("#ismediaRequiredCheckbox_" + mediaId).is(":checked");

           mediaReports.push({
               Id: mediaId,
               Selected: $(this).is(":checked"),
               Required: isRequired,
               CameraCaptureMode: getCameraCaptureMode($container)
           });
       });

    $.confirm({
        title: 'Confirm Save',
        icon: 'fas fa-edit',
        content: 'Are you sure you want to save this location?',
        type: 'green',
        buttons: {
            confirm: {
                text: 'Yes',
                btnClass: 'btn-success',
                action: function () {
                    $spinner.removeClass("hidden");
                    $btn.prop("disabled", true).html('<i class="fas fa-sync fa-spin"></i> Save');
                    $.ajax({
                        url: '/ReportTemplate/SaveLocation',
                        type: 'POST',
                        contentType: 'application/json',
                        headers: {
                            'X-CSRF-TOKEN': $('input[name="__RequestVerificationToken"]').val()
                        },
                        data: JSON.stringify({
                            TemplateId: templateId,
                            LocationName: locationName,
                            LocationId: locationId,
                            FaceIds: faceIds,
                            DocumentIds: documentIds,
                            MediaReports: mediaReports
                        }),
                        success: function (response) {
                            if (response.success) {
                                $.alert({
                                    title: '<span class="i-green"><i class="fas fa-check-circle"></i></span> Success',
                                    content: response.message || "Location saved successfully!",
                                    type: "green",
                                });
                            } else {
                                $.alert({
                                    title: '<span class="i-orangered"><i class="fas fa-exclamation-triangle"></i></span> Error!',
                                    content: response.message || "Failed to save location.",
                                    type: "red"
                                });
                            }
                        },
                        error: function (xhr) {
                            console.error(xhr.responseText);
                            $.alert({
                                title: '<span class="i-orangered"><i class="fas fa-exclamation-triangle"></i></span> Error!',
                                content: "An error occurred while saving.",
                                type: "red"
                            });
                        },
                        complete: function () {
                            $spinner.addClass("hidden");
                            $btn.prop("disabled", false).html('<i class="fas fa-edit me-1"></i> Save');
                        }
                    });
                }
            },
            cancel: function () {
                // Do nothing
            }
        }
    });
   });

    //Clone location
    $(document).on("click", ".clone-location-btn", function (e) {
        e.preventDefault();

        var $btn = $(this);
        var locationId = $btn.data("locationid");
        var reportTemplateId = $btn.data("reporttemplateid");
        var $spinner = $(".submit-progress"); // global spinner (you already have this)

        if (!locationId || !reportTemplateId) {
            $.alert({
                title: "Error",
                content: "Missing location or template ID.",
                type: "red"
            });
            return;
        }

        $.confirm({
            title: ' Clone Location',
            content: 'Are you sure you want to clone this location?',
            icon: 'fas fa-copy',
            type: 'dark',
            buttons: {
                Yes: {
                    text: 'Yes, Clone',
                    btnClass: 'btn-dark',
                    action: function () {
                        $spinner.removeClass("hidden");
                        $btn.prop("disabled", true).html('<i class="fas fa-sync fa-spin"></i> Clone.');
                        $.ajax({
                            url: '/ReportTemplate/CloneLocation',
                            type: 'POST',
                            data: {
                                locationId: locationId,
                                reportTemplateId: reportTemplateId,
                                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val(),
                            },
                            success: function (response) {
                                if (response.success) {
                                    $.alert({
                                        title: "Cloned",
                                        content: "Location cloned successfully! Reloading...",
                                        type: "green",
                                        buttons: {
                                            OK: function () {
                                                location.reload(); // reload page after confirmation
                                            }
                                        }
                                    });
                                } else {
                                    $.alert({
                                        title: "Failed",
                                        content: response.message || "Unable to clone location.",
                                        type: "red"
                                    });
                                }
                            },
                            error: function (xhr) {
                                $.alert({
                                    title: "Error",
                                    content: "Server error while cloning location.",
                                    type: "red"
                                });
                            },
                            complete: function () {
                                $spinner.addClass("hidden");
                                $btn.prop("disabled", false).html('<i class="fas fa-clone"></i> Clone');
                            }
                        });
                    },
                },
                Cancel: function () { }
            }
        });
    });

    //activate
    $(document).on('click', '.activation-btn', function (e) {
        e.preventDefault();
        var id = $(this).data('id');
        var $btn = $(this);

        if (!id) {
            $.alert({
                title: "Error",
                content: "Missing templateId ID.",
                type: "red"
            });
            return;
        }
        var $spinner = $(".submit-progress"); // global spinner (you already have this)

        $.confirm({
            title: 'Confirm Activation',
            icon: 'fas fa-flash',
            content: 'Are you sure you want to activate this report?',
            type: 'green',
            buttons: {
                confirm: {
                    text: 'Yes, Activate',
                    btnClass: 'btn-green',
                    action: function () {
                        $spinner.removeClass("hidden");
                        $btn.prop("disabled", true).html('<i class="fas fa-sync fa-spin"></i> Activate');
                        $.ajax({
                            url: '/ReportTemplate/ActivateDetail',
                            type: 'POST',
                            data: {
                                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val(),
                                id: id
                            },
                            success: function (response) {
                                if (response.success) {
                                    $.alert({
                                        title: '<span class="i-green"> <i class="fas fas fa-flash"></i> </span> Activated!',
                                        content: response.message,
                                        type: 'green',
                                        buttons: {
                                            OK: {
                                                btnClass: 'btn-green',
                                                icon: 'fa-flash',
                                                action: function () {
                                                }
                                            }
                                        }
                                    });
                                } else {
                                    $.alert({
                                        title: '<span class="i-orangered"> <i class="fas fa-exclamation-triangle"></i> </span> Error!',
                                        content: response.message,
                                        type: 'red'
                                    });
                                }
                            },
                            error: function () {
                                $.alert({
                                    title: '<span class="i-orangered"> <i class="fas fa-exclamation-triangle"></i> </span> Error!',
                                    content: 'Something went wrong while activating the report.',
                                    type: 'red'
                                });
                            },
                            complete: function () {
                                // ✅ Re-enable button and restore text
                                $spinner.addClass("hidden");
                                $btn
                                    .removeClass('btn-success activation-btn')
                                    .addClass('btn-outline-success')
                                    .prop('disabled', true)
                                    .attr('title', 'The template is active')
                                    .html('<i class="fas fa-flash igreen"></i> <b>Active</b> <i class="fas fa-check-circle"></i>');
                            }
                        });
                    }
                },
                cancel: {
                    text: 'Cancel',
                    btnClass: 'btn-default'
                }
            }
        });
    });
});

function safeText(v) {
    // keep this as your current implementation: it encodes any HTML special chars
    return $('<div>').text(v || "").text();
}

function sanitizeId(v) {
    if (v === null || v === undefined) return "";
    // allow only alphanum, underscore, hyphen (adjust to your id format if needed)
    return String(v).replace(/[^a-zA-Z0-9_\-]/g, "");
}