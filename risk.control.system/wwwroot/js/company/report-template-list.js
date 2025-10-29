$(document).ready(function () {

    var table =$('#reportTemplatesTable').DataTable({
        processing: true,
        serverSide: true,
        language: {
            loadingRecords: '&nbsp;',
            processing: '<i class="fas fa-sync fa-spin fa-4x fa-fw"></i><span class="sr-only">Loading...</span>'
        },
        ajax: {
            url: '/ReportTemplate/GetReportTemplates',
            type: 'POST',
            data: function (d) {
                d.insuranceType = $('#caseTypeFilter').val(); // <--- sends filter value
            }
        },
        columns: [
            { data: 'id', "bVisible": false },
            { data: 'name' },
            { data: 'insuranceType' },
            {
                data: 'isActive',
                render: function (data) {
                    return data
                        ? '<span class="badge bg-success"><i class="fas fa-check-circle"></i>  Active</span>'
                        : '<span class="badge bg-secondary"><i class="fas fa-times-circle"></i> Inactive</span>';
                }
            },
            {
                data: 'createdOn',
                render: function (data) {
                    return new Date(data).toLocaleDateString();
                }
            },
            { data: 'locations' },
            { data: 'faceCount' },
            { data: 'docCount' },
            { data: 'mediaCount' },
            { data: 'questionCount' },
            {
                data: null,
                orderable: false,
                searchable: false,
                render: function (data, type, row) {
                    // Button HTML setup
                    let activateBtn = row.isActive
                        ? `<button class="btn btn-xs btn-outline-success" disabled><i class="fas fa-flash"></i> Active</button>`
                        : `<button class="btn btn-xs btn-success activate-btn" data-id="${row.id}" data-insurancetype="${row.insuranceType}"><i class="fas fa-flash"></i> Activate</button>`;

                    let editBtn = `<button class="btn btn-xs btn-warning edit-template" data-id="${row.id}"><i class="fas fa-edit"></i> Edit</button>`;
                    let cloneBtn = `<button class="btn btn-xs btn-secondary  clone-btn" data-id="${row.id}"><i class="fas fa-copy"></i> Clone</button>`;
                    let deleteBtn = row.isActive ?
                        `<button class="btn btn-xs btn-danger" disabled><i class="fas fa-trash"></i> Delete</button>` : `<button class="btn btn-xs btn-danger delete-template" data-id="${row.id}"><i class="fas fa-trash"></i> Delete</button>`;

                    return `${activateBtn} ${cloneBtn} ${editBtn} ${deleteBtn}`;
                }
            }
        ],
        order: [[0, 'desc']]
    });

    $('#caseTypeFilter').on('change', function () {
        table.ajax.reload(); // Reload the table when the filter is changed
    });
    $('#refreshTable').click(function () {
        var $icon = $('#refreshIcon');
        if ($icon) {
            $icon.addClass('fa-spin');
        }
        table.ajax.reload(null, false); // false => Retains current page
    });

    table.on('xhr.dt', function () {
        $('#refreshIcon').removeClass('fa-spin');
    });

    // Add Question button click event
    $(document).on('click', '.add-question-btn', function () {
        var locationId = $(this).data('locationid');

        // Reset form and set location
        $('#questionAddForm')[0].reset();
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

    // helper to escape text (avoid XSS when injecting server text)
    function escapeHtml(text) {
        if (!text) return "";
        return text
            .toString()
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#039;");
    }

    // toggle options container when type changes (optional)
    $(document).on('change', '#QuestionType', function () {
        const t = $(this).val();
        if (t === 'dropdown' || t === 'radiobutton' || t === 'checkbox') {
            $('#optionsContainer').removeClass('d-none').show();
        } else {
            $('#optionsContainer').addClass('d-none').hide();
        }
    });

    // Handle Question Add form submission
    $(document).on('submit', '#questionAddForm', function (e) {
        e.preventDefault();

        var locationId = $('#questionAddForm input[name="LocationId"]').val();
        var optionsInput = $('#optionsInput').val();
        var newQuestionText = $('#QuestionText').val();
        var newQuestionType = $('#QuestionType').val();
        // <-- THIS IS THE FIX: check checked state
        var isRequired = $('#isRequired').is(':checked'); // returns true/false

        $.ajax({
            url: '/ReportTemplate/AddQuestion',
            method: 'POST',
            data: {
                icheckifyAntiforgery: $('input[name="icheckifyAntiforgery"]').val(),
                locationId: locationId,
                optionsInput: optionsInput,
                newQuestionText: newQuestionText,
                newQuestionType: newQuestionType,
                isRequired: isRequired
            },
            success: function (response) {
                if (response.success) {
                    // close correct modal id
                    $('#addQuestionModal').modal('hide');

                    // show jConfirm success dialog
                    $.confirm({
                        title: '<span class="i-green"> <i class="fas fa-question"></i> </span> Question',
                        content: 'Question has been added successfully.',
                        type: 'green',
                        buttons: {
                            ok: function () { /* do nothing */ }
                        }
                    });

                    // optionally append/prepend the new question to UI so user sees it immediately
                    var q = response.updatedQuestion;
                    if (q && locationId) {
                        var requiredHtml = q.isRequired ? ' <span class="required-asterisk" title="Required field">*</span>' : '';
                        var optionsHtml = '';
                        if (q.questionType && q.questionType.toLowerCase() !== 'text' && q.options) {
                            var opts = (q.options || '').split(',').map(function (o) { return '<span class="badge bg-light text-dark border me-1">' + escapeHtml(o.trim()) + '</span>'; }).join(' ');
                            optionsHtml = '<div class="mt-4">' + opts + '</div>';
                        }
                        var newHtml = '<li class="mb-2">' +
                            '<div class="border rounded p-2 bg-light">' +
                            '<div class="row">' +
                            '<div class="col-md-11">' +
                            '<span>' + escapeHtml(q.questionText) + '</span>' + requiredHtml +
                            ' <small class="text-muted">[' + escapeHtml(q.questionType) + ']</small>' +
                            optionsHtml +
                            '</div>' +
                            '<div class="mt-2">' +
                            '<button class="btn btn-sm btn-outline-danger delete-question-btn" data-questionid="' + q.id + '" data-locationid="' + locationId + '">' +
                            '<i class="fas fa-trash me-1"></i><small> Delete </small>' +
                            '</button>' +
                            '</div>' +
                            '</div>' +
                            '</div>' +
                            '</li>';

                        // find the add button for this location and insert into its question list
                        var $addBtn = $('button.add-question-btn[data-locationid="' + locationId + '"]');
                        var $list = $addBtn.closest('.col-md-9').find('ul.list-unstyled').first();
                        if ($list.length) {
                            $list.append(newHtml);
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
                                icheckifyAntiforgery: $('input[name="icheckifyAntiforgery"]').val(),
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
                                    icheckifyAntiforgery: $('input[name="icheckifyAntiforgery"]').val(),
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
        var $card = $btn.closest(".card"); // scope to this location card
        var $spinner = $(".submit-progress"); // global spinner (you already have this)

        // ✅ Get Location Name
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
        // Collect AgentId
        var agentId = null;
        var $agentCheckbox = $card.find("input[id^='agent_']");
        if ($agentCheckbox.length) {
            agentId = {
                Id: $agentCheckbox.attr("id").replace("agent_", ""),
                Selected: $agentCheckbox.is(":checked")
            };
        }

        // Collect selected FaceIds
        var faceIds = [];
        $card.find("input[id^='face_']").each(function () {
            faceIds.push({
                Id: $(this).attr("id").replace("face_", ""),
                Selected: $(this).is(":checked")
            });
        });

        // Collect selected DocumentIds
        var documentIds = [];
        $card.find("input[id^='doc_']").each(function () {
            documentIds.push({
                Id: $(this).attr("id").replace("doc_", ""),
                Selected: $(this).is(":checked")
            });
        });

        // Collect selected MediaReports
        var mediaReports = [];
        $card.find("input[id^='media_']").each(function () {
            mediaReports.push({
                Id: $(this).attr("id").replace("media_", ""),
                Selected: $(this).is(":checked")
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
                        // Disable button and show spinner
                        $btn.prop("disabled", true).html('<i class="fas fa-sync fa-spin"></i> Save');
                        $.ajax({
                            url: '/ReportTemplate/SaveLocation',
                            type: 'POST',
                            contentType: 'application/json',
                            headers: {
                                'X-CSRF-TOKEN': $('input[name="icheckifyAntiforgery"]').val()
                            },
                            data: JSON.stringify({
                                TemplateId: templateId,
                                LocationName: locationName,
                                AgentId: agentId,
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
                                // ✅ Re-enable button and restore text
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
                                icheckifyAntiforgery: $('input[name="icheckifyAntiforgery"]').val(),
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

    //Activate the report
    $(document).on('click', '.activate-btn', function (e) {
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
                        url: '/ReportTemplate/Activate',
                        type: 'POST',
                        data: {
                            icheckifyAntiforgery: $('input[name="icheckifyAntiforgery"]').val(),
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
                                                $('#reportTemplatesTable').DataTable().ajax.reload();
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
                            //$btn.prop("disabled", false).html('<i class="fas fa-flash"></i> Activate');
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

    var hasClone = true;
    $(document).on('click', '.clone-btn', function (e) {
        e.preventDefault();
        var id = $(this).data('id');
        var url = $(this).attr("href");
        if (hasClone) {
            var $btn = $(this);
            var $spinner = $(".submit-progress"); // global spinner (you already have this)

            $.confirm({
                title: 'Confirm Clone',
                content: 'Do you want to clone this template?',
                icon: 'fas fa-copy',
                type: 'dark',
                typeAnimated: true,
                buttons: {
                    confirm: {
                        text: 'Yes, Clone',
                        btnClass: 'btn-dark',
                        action: function () {
                            $spinner.removeClass("hidden");
                            $btn.prop("disabled", true).html('<i class="fas fa-sync fa-spin"></i> Clone');
                            $.ajax({
                                url: '/ReportTemplate/CloneDetails',
                                type: 'POST',
                                data: {
                                    icheckifyAntiforgery: $('input[name="icheckifyAntiforgery"]').val(),
                                    templateId: id
                                },
                                success: function (response) {
                                    if (response.success) {
                                        $.alert({
                                            title: '<span class="i-gray"> <i class="fas fa-copy"></i> </span> Cloned!',
                                            content: response.message,
                                            type: 'dark',
                                            buttons: {
                                                OK: {
                                                    btnClass: 'btn-dark',
                                                    icon: 'fa-copy',
                                                    action: function () {
                                                        $('#reportTemplatesTable').DataTable().ajax.reload();
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
                                        content: 'Something went wrong while cloning the report.',
                                        type: 'red'
                                    });
                                },
                                complete: function () {
                                    // ✅ Re-enable button and restore text
                                    $spinner.addClass("hidden");
                                    $btn.prop("disabled", false).html('<i class="fas fa-flash"></i> Activate');
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
        }
    });

    //edit template
    $(document).on('click', '.edit-template', function (e) {
        var id = $(this).data('id');
        $("body").addClass("submit-progress-bg");
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);
        disableAllInteractiveElements();
        var $btn = $(this);
        $btn.prop('disabled', true);         // disable button
        $btn.addClass('disabled');           // add visual Bootstrap disabled style
        $btn.html('<i class="fas fa-sync fa-spin"></i> Edit'); // show spinner feedback
        location.href = "/ReportTemplate/Details/" + id;
    });

    //delete template
    $(document).on('click', '.delete-template', function () {
        var id = $(this).data("id");
        var row = $(this).closest("tr");
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
            title: 'Confirm Deletion',
            content: 'Are you sure you want to delete this template?',
            type: 'red',
            buttons: {
                confirm: {
                    text: 'Yes, Delete',
                    btnClass: 'btn-red',
                    action: function () {
                        $spinner.removeClass("hidden");
                        $btn.prop("disabled", true).html('<i class="fas fa-sync fa-spin"></i> Delete');
                        $.ajax({
                            url: '/ReportTemplate/DeleteTemplate',
                            type: 'POST',
                            data: {
                                icheckifyAntiforgery: $('input[name="icheckifyAntiforgery"]').val(),
                                id: id
                            },
                            success: function (response) {
                                if (response.success) {
                                    $.alert({
                                        title: '<span class="i-orangered"> <i class="fas fa-trash"></i> </span> Deleted!',
                                        content: response.message,
                                        type: 'red',
                                        buttons: {
                                            OK: {
                                                btnClass: 'btn-red',
                                            }
                                        }
                                    });
                                    row.fadeOut(500, function () {
                                        $(this).remove();
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
                                    content: 'Something went wrong. Please try again.',
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
                cancel: {
                    text: 'Cancel',
                        btnClass: 'btn-default'
                }
            }
        });
    });

    //Activate the report
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
                            url: '/ReportTemplate/Activate',
                            type: 'POST',
                            data: {
                                icheckifyAntiforgery: $('input[name="icheckifyAntiforgery"]').val(),
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
                                                    location.reload();
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
    $('#claim-tab').click(function () {
            $('#claim-content').addClass('show active');
            $('#underwriting-content').removeClass('show active');
        });

    $('#underwriting-tab').click(function () {
        $('#underwriting-content').addClass('show active');
        $('#claim-content').removeClass('show active');
    });
});