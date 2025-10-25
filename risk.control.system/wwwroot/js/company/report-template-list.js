document.addEventListener("DOMContentLoaded", function () {
    const headers = document.querySelectorAll('.location-header');

    headers.forEach(header => {
        header.addEventListener('click', function () {
            this.classList.toggle('active');
            const content = this.nextElementSibling;
            if (content.style.display === "block") {
                content.style.display = "none";
            } else {
                content.style.display = "block";
            }
        });
    });
});
$(document).ready(function () {
    
    // Add Question button click event
    $(document).on('click', '.add-question-btn', function () {
        var locationId = $(this).data('locationid');
        // Open modal to add Question
        var questionType = $('#QuestionType');
        if (questionType === 'dropdown' || t === 'radiobutton' || t === 'checkbox') {
            $('#optionsContainer').removeClass('hidden').show();
        } else {
            $('#optionsContainer').addClass('hidden').hide();
        }
        $('#questionAddForm')[0].reset();
        $('#addQuestionModal').find('input[name="LocationId"]').val(locationId);
        $('#QuestionText').focus();
        $('#addQuestionModal').modal('show');
    });

    // Handle Question update button click
    $(document).on('click', '.update-question-btn', function () {
        var questionId = $(this).data('questionid');
        var locationId = $(this).closest('.col-md-8').find('.add-question-btn').data('locationid');

        $.ajax({
            url: '/ReportTemplate/GetQuestionDetails',
            method: 'GET',
            data: { questionId: questionId },
            success: function (response) {
                if (response.success) {
                    $('#questionUpdateModal').find('input[name="QuestionId"]').val(response.question.id);
                    $('#questionUpdateModal').find('input[name="LocationId"]').val(locationId);
                    $('#questionUpdateModal').find('input[name="QuestionText"]').val(response.question.questionText);
                    $('#questionUpdateModal').find('select[name="QuestionType"]').val(response.question.questionType);
                    $('#questionUpdateModal').find('#isRequired').prop(
                        'checked',
                        response.question.isRequired === true || response.question.isRequired === "true"
                    );

                    let type = response.question.questionType?.toLowerCase();

                    if (type === "dropdown" || type === "radiobutton" || type === "checkbox") {
                        let options = response.question.options?.split(',').map(o => o.trim()) || [];
                        let $optionsSelect = $('#Options');
                        $optionsSelect.empty();
                        options.forEach(opt => {
                            $optionsSelect.append(`<option value="${opt}" selected>${opt}</option>`);
                        });

                        $('#optionsContainer').removeClass('hidden');
                        $('#dateContainer').addClass('hidden');
                    }
                    else if (type === "date") {
                        $('#dateContainer').removeClass('hidden');
                        $('#optionsContainer').addClass('hidden');
                    }
                    else {
                        $('#optionsContainer, #dateContainer').addClass('hidden');
                    }

                    $('#questionUpdateModal').modal('show');
                }
            }
        });
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
            $('#optionsContainer').removeClass('hidden').show();
        } else {
            $('#optionsContainer').addClass('hidden').hide();
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
                        title: 'Question added',
                        content: 'Question has been added successfully.',
                        type: 'green',
                        buttons: {
                            ok: function () { /* do nothing */ },
                            addAnother: {
                                text: 'Add another',
                                action: function () {
                                    var questionType = $('#QuestionType');
                                    if (questionType === 'dropdown' || t === 'radiobutton' || t === 'checkbox') {
                                        $('#optionsContainer').removeClass('hidden').show();
                                    } else {
                                        $('#optionsContainer').addClass('hidden').hide();
                                    }
                                    $('#questionAddForm')[0].reset();
                                    $('#addQuestionModal').modal('show');
                                }
                            }
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
                            '<div class="col-md-1">' +
                            '<button class="btn btn-sm btn-outline-danger delete-question-btn" data-questionid="' + q.id + '">' +
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

    // Handle Question update form submission
    $(document).on('submit', '#questionUpdateForm', function (e) {
        e.preventDefault();

        var questionId = $('input[name="QuestionId"]').val();
        var locationId = $('input[name="LocationId"]').val();
        var newQuestionText = $('#questionUpdateModal').find('input[name="NewQuestionText"]').val();
        var newQuestionType = $('#questionUpdateModal').find('select[name="NewQuestionType"]').val();

        $.ajax({
            url: '/ReportTemplate/UpdateQuestion',  // Endpoint to update Question
            method: 'POST',
            data: {
                icheckifyAntiforgery: $('input[name="icheckifyAntiforgery"]').val(),
                id: questionId,
                newQuestionText: newQuestionText,
                newQuestionType: newQuestionType
            },
            success: function (response) {
                if (response.success) {
                    $('#questionUpdateModal').modal('hide');
                    // Update the UI with the new data
                    $('button[data-questionid="' + questionId + '"]').closest('li').find('.question-text').text(response.updatedQuestion.QuestionText);
                    $('button[data-questionid="' + questionId + '"]').closest('li').find('.question-type').text(response.updatedQuestion.QuestionType);
                }
            }
        });
    });

    //Delete question
    $(document).on("click", ".delete-question-btn", function (e) {
        e.preventDefault();

        var questionId = $(this).data("questionid");
        var locationId = $(this).data("locationid");
        var $row = $(this).closest("li"); // question row

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
        var locationDeletable = $('#locationCount').val() > 1;
        if (!locationDeletable) {
            $.alert({
                title: '<span class="i-orangered"> <i class="fas fa-exclamation-triangle"></i> </span> Error!',
                content: 'Single Location not DELETED.',
                type: 'red'
            });
        } else {
            var locationId = $(this).data("locationid");
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

        var locationId = $(this).data("locationid");
        var $card = $(this).closest(".card"); // scope to this location card

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

        // Collect selected MediaReports (assuming also `doc_` prefix, adjust if needed)
        var mediaReports = [];
        $card.find("input[id^='media_']").each(function () {
            mediaReports.push({
                Id: $(this).attr("id").replace("media_", ""),
                Selected: $(this).is(":checked")
            });
        });

        $.ajax({
            url: '/ReportTemplate/SaveLocation',
            type: 'POST',
            contentType: 'application/json',
            headers: {
                'X-CSRF-TOKEN': $('input[name="icheckifyAntiforgery"]').val()
            },
            data: JSON.stringify({
                
                AgentId: agentId,
                LocationId: locationId,
                FaceIds: faceIds,
                DocumentIds: documentIds,
                MediaReports: mediaReports
            }),
            success: function (response) {
                if (response.success) {
                    $.alert({
                        title: '<span class="i-green"><i class="fas fa-edit"></i> </span> Success',
                        content: "Location saved successfully!",
                        type: "green"
                    });
                } else {
                    $.alert({
                        title: '<span class="i-orangered"> <i class="fas fa-exclamation-triangle"></i> </span> Error!',
                        content: response.message || "Failed to save location.",
                        type: "red"
                    });
                }
            },
            error: function (xhr) {
            console.error(xhr.responseText);
            $.alert({
                title: '<span class="i-orangered"> <i class="fas fa-exclamation-triangle"></i> </span> Error!',
                content: "An error occurred while saving.",
                type: "red"
            });
        }
        });
    });

    //Activate the report
    $(document).on('click', '.activate-btn', function (e) {
        e.preventDefault();
        var id = $(this).data('id');
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
                                            location.href = "/ReportTemplate/Profile";
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
    $(document).on('click', '.clone-template', function (e) {
        e.preventDefault();
        var url = $(this).attr("href");
        if (hasClone) {
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
                            hasClone = false;
                            $("body").addClass("submit-progress-bg");
                            setTimeout(function () {
                                $(".submit-progress").removeClass("hidden");
                            }, 1);
                            disableAllInteractiveElements();
                            window.location.href = url; // proceed to clone
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
        $("body").addClass("submit-progress-bg");
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);
        disableAllInteractiveElements();
        var $btn = $(this);
        $btn.prop('disabled', true);         // disable button
        $btn.addClass('disabled');           // add visual Bootstrap disabled style
        $btn.html('<i class="fas fa-sync fa-spin"></i> Edit'); // show spinner feedback
    });

    //delete template
    $(document).on('click', '.delete-template', function () {
        var id = $(this).data("id");
        var row = $(this).closest("tr");

        $.confirm({
            title: 'Confirm Deletion',
            content: 'Are you sure you want to delete this template?',
            type: 'red',
            buttons: {
                confirm: {
                    text: 'Yes, Delete',
                    btnClass: 'btn-red',
                    action: function () {
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