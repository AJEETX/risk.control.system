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
    // Handle FaceId update button click
    $(document).on('click', '.update-faceid-btn', function () {
        var faceId = $(this).data('faceid');
        var locationId = $(this).closest('tr').find('.add-faceid-btn').data('locationid');

        // Fetch current FaceId details
        $.ajax({
            url: '/ReportTemplate/GetFaceIdDetails', // Now we have the correct endpoint
            method: 'GET',
            data: { faceId: faceId },
            success: function (response) {
                if (response.success) {
                    $('#faceIdUpdateModal').find('input[name="FaceId"]').val(response.faceId.Id);
                    $('#faceIdUpdateModal').find('input[name="LocationId"]').val(locationId);
                    $('#faceIdUpdateModal').find('input[name="NewFaceIdName"]').val(response.faceId.IdIName);
                    $('#faceIdUpdateModal').find('select[name="NewReportType"]').val(response.faceId.ReportType);
                    $('#faceIdUpdateModal').modal('show');
                }
            }
        });
    });

    // Handle DocumentId update button click
    $(document).on('click', '.update-docid-btn', function () {
        var docId = $(this).data('docid');
        var locationId = $(this).closest('tr').find('.add-docid-btn').data('locationid');

        // Fetch current DocumentId details
        $.ajax({
            url: '/ReportTemplate/GetDocumentIdDetails', // Now we have the correct endpoint
            method: 'GET',
            data: { docId: docId },
            success: function (response) {
                if (response.success) {
                    $('#documentIdUpdateModal').find('input[name="DocumentId"]').val(response.documentId.Id);
                    $('#documentIdUpdateModal').find('input[name="LocationId"]').val(locationId);
                    $('#documentIdUpdateModal').find('input[name="NewDocumentIdName"]').val(response.documentId.IdIName);
                    $('#documentIdUpdateModal').find('select[name="NewDocumentType"]').val(response.documentId.DocumentType);
                    $('#documentIdUpdateModal').modal('show');
                }
            }
        });
    });

    // Handle FaceId update form submission
    $(document).on('submit', '#faceIdUpdateForm', function (e) {
        e.preventDefault();

        var faceId = $('input[name="FaceId"]').val();
        var locationId = $('input[name="LocationId"]').val();
        var newName = $('#faceIdUpdateModal').find('input[name="NewFaceIdName"]').val();
        var newReportType = $('#faceIdUpdateModal').find('select[name="NewReportType"]').val();

        $.ajax({
            url: '/ReportTemplate/UpdateFaceId',
            method: 'POST',
            data: {
                id: faceId,
                newName: newName,
                newReportType: newReportType
            },
            success: function (response) {
                if (response.success) {
                    $('#faceIdUpdateModal').modal('hide');
                    // Update UI with new data
                    $('button[data-faceid="' + faceId + '"]').closest('li').find('span').text(response.updatedFaceId.Name);
                    $('button[data-faceid="' + faceId + '"]').closest('li').find('span').attr('data-report-type', response.updatedFaceId.ReportType);
                }
            }
        });
    });

    // Handle DocumentId update form submission
    $(document).on('submit', '#documentIdUpdateForm', function (e) {
        e.preventDefault();

        var docId = $('input[name="DocumentId"]').val();
        var locationId = $('input[name="LocationId"]').val();
        var newName = $('#documentIdUpdateModal').find('input[name="NewDocumentIdName"]').val();
        var newDocumentType = $('#documentIdUpdateModal').find('select[name="NewDocumentType"]').val();

        $.ajax({
            url: '/ReportTemplate/UpdateDocumentId',
            method: 'POST',
            data: {
                id: docId,
                newName: newName,
                newDocumentType: newDocumentType
            },
            success: function (response) {
                if (response.success) {
                    $('#documentIdUpdateModal').modal('hide');
                    // Update UI with new data
                    $('button[data-docid="' + docId + '"]').closest('li').find('span').text(response.updatedDocumentId.Name);
                    $('button[data-docid="' + docId + '"]').closest('li').find('span').attr('data-document-type', response.updatedDocumentId.DocumentType);
                }
            }
        });
    });
    // Add FaceId button click event
    $(document).on('click', '.add-faceid-btn', function () {
        var locationId = $(this).data('locationid');
        // Open modal to add FaceId
        $('#addFaceIdModal').find('input[name="LocationId"]').val(locationId);
        $('#addFaceIdModal').modal('show');
    });

    // Add DocumentId button click event
    $(document).on('click', '.add-docid-btn', function () {
        var locationId = $(this).data('locationid');
        // Open modal to add DocumentId
        $('#addDocumentIdModal').find('input[name="LocationId"]').val(locationId);
        $('#addDocumentIdModal').modal('show');
    });

    // Add Question button click event
    $(document).on('click', '.add-question-btn', function () {
        var locationId = $(this).data('locationid');
        // Open modal to add Question
        $('#addQuestionModal').find('input[name="LocationId"]').val(locationId);
        $('#addQuestionModal').modal('show');
    });

    $(document).on('submit', '#addFaceIdForm', function (e) {
        e.preventDefault();

        var locationId = $('input[name="LocationId"]').val();
        var faceIdName = $('#faceIdName').val();
        var reportType = $('#faceIdReportType').val();

        $.ajax({
            url: '/ReportTemplate/AddFaceId',  // Change this to the relevant URL
            method: 'POST',
            data: {
                LocationId: locationId,
                IdIName: faceIdName,
                ReportType: reportType
            },
            success: function (response) {
                // Close modal and update UI with new FaceId
                $('#addFaceIdModal').modal('hide');
                // Optionally update the FaceIds section or reload the page
            },
            error: function (error) {
                console.error('Error adding FaceId:', error);
            }
        });
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
                            '<div class="col-md-10">' +
                            '<span>' + escapeHtml(q.questionText) + '</span>' + requiredHtml +
                            ' <small class="text-muted">[' + escapeHtml(q.questionType) + ']</small>' +
                            optionsHtml +
                            '</div>' +
                            '<div class="col-md-2">' +
                            '<button class="btn btn-sm btn-outline-danger delete-question-btn" data-questionid="' + q.Id + '">' +
                            '<i class="fas fa-trash me-1"></i> Delete' +
                            '</button>' +
                            '</div>' +
                            '</div>' +
                            '</div>' +
                            '</li>';

                        // find the add button for this location and insert into its question list
                        var $addBtn = $('button.add-question-btn[data-locationid="' + locationId + '"]');
                        var $list = $addBtn.closest('.col-md-8').find('ul.list-unstyled').first();
                        if ($list.length) {
                            $list.prepend(newHtml);
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