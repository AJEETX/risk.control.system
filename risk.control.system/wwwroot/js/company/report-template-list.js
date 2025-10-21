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
        var $row = $(this).closest("li"); // question row

        $.confirm({
            title: 'Confirm Delete',
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
                            data: { id: questionId },
                            success: function (response) {
                                if (response.success) {
                                    $row.remove(); // remove from UI
                                    $.alert({
                                        title: 'Deleted',
                                        content: 'Question has been deleted successfully!',
                                        type: 'green'
                                    });
                                } else {
                                    $.alert({
                                        title: 'Error',
                                        content: response.message || 'Failed to delete question.',
                                        type: 'red'
                                    });
                                }
                            },
                            error: function () {
                                $.alert({
                                    title: 'Error',
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

        var locationId = $(this).data("locationid");
        var $card = $(this).closest(".col-12"); // outer card for that location

        $.confirm({
            title: 'Confirm Delete',
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
                            data: { id: locationId },
                            success: function (response) {
                                if (response.success) {
                                    $card.remove(); // remove location from UI
                                    $.alert({
                                        title: 'Deleted',
                                        content: 'Location deleted successfully!',
                                        type: 'green'
                                    });
                                } else {
                                    $.alert({
                                        title: 'Error',
                                        content: response.message || 'Failed to delete location.',
                                        type: 'red'
                                    });
                                }
                            },
                            error: function () {
                                $.alert({
                                    title: 'Error',
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
                        title: "Success",
                        content: "Location saved successfully!",
                        type: "green"
                    });
                } else {
                    $.alert({
                        title: "Error",
                        content: response.message || "Failed to save location.",
                        type: "red"
                    });
                }
            },
            error: function () {
                $.alert({
                    title: "Error",
                    content: "An error occurred while saving.",
                    type: "red"
                });
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