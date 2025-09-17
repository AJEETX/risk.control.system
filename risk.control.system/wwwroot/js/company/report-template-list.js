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
        var locationId = $(this).closest('tr').find('.add-question-btn').data('locationid');

        // Fetch current Question details
        $.ajax({
            url: '/ReportTemplate/GetQuestionDetails', // Correct endpoint to fetch Question details
            method: 'GET',
            data: { questionId: questionId },
            success: function (response) {
                if (response.success) {
                    $('#questionUpdateModal').find('input[name="QuestionId"]').val(response.question.id);
                    $('#questionUpdateModal').find('input[name="LocationId"]').val(locationId);
                    $('#questionUpdateModal').find('input[name="QuestionText"]').val(response.question.questionText);
                    $('#questionUpdateModal').find('select[name="QuestionType"]').val(response.question.questionType);
                    $('#questionUpdateModal')
                        .find('select[name="QuestionType"]')
                        .val(response.question.questionType?.toLowerCase());

                    let type = response.question.questionType?.toLowerCase();

                    if (type === "dropdown" || type === "radiobutton" || type === "checkbox") {
                        let options = response.question.options.split(',').map(o => o.trim());

                        let $optionsSelect = $('#Options');
                        $optionsSelect.empty(); // clear existing
                        options.forEach(opt => {
                            let isSelected = response.question.options?.split(',').includes(opt);
                            $optionsSelect.append(
                                `<option value="${opt}" ${isSelected ? "selected" : ""}>${opt}</option>`
                            );
                        });
                        $('#optionsContainer').removeClass('hidden');
                        $('#dateContainer').addClass('hidden');
                    } else if (response.question.questionType === "date") {
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

    $('#QuestionType').on('change', function () {
        let type = $(this).val();

        if (type === "dropdown" || type === "radiobutton" || type === "checkbox") {
            $('#optionsContainer').removeClass('hidden');
            $('#dateContainer').addClass('hidden');
        }
        else if (type === "date") {
            $('#dateContainer').removeClass('hidden');
            $('#optionsContainer').addClass('hidden');
        }
        else {
            // text or empty
            $('#optionsContainer, #dateContainer').addClass('hidden');
        }
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

    $('#claim-tab').click(function () {
            $('#claim-content').addClass('show active');
            $('#underwriting-content').removeClass('show active');
        });

        $('#underwriting-tab').click(function () {
            $('#underwriting-content').addClass('show active');
            $('#claim-content').removeClass('show active');
        });
});