$(document).ready(function () {
    // 1. Fetch dynamic column headers
    $.get('/Claim/GetFormFields', function (fields) {
        const headerRow = $('#tableHeaders');

        const columnsConfig = [
            { data: 'id', className: 'align-middle' },
            { data: 'submittedAt', className: 'align-middle' }
        ];

        // 2. Loop through and map dynamic fields
        fields.forEach(field => {
            headerRow.append(`<th>${field.label}</th>`);

            columnsConfig.push({
                data: 'fields',
                orderable: false,
                className: 'align-middle',
                render: function (data, type, row) {
                    const matchedField = data.find(f => f.formFieldId === field.id);
                    if (!matchedField || !matchedField.value) {
                        return '<span class="text-muted small">-</span>';
                    }

                    if (matchedField.type === 'date') {
                        const dateParts = matchedField.value.split('-');
                        if (dateParts.length === 3) {
                            const months = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];
                            return `${dateParts[2]}-${months[parseInt(dateParts[1], 10) - 1]}-${dateParts[0]}`;
                        }
                    }

                    if (matchedField.type === 'file') {
                        const isImg = /\.(jpg|jpeg|png|gif|webp)$/i.test(matchedField.value);
                        if (isImg) {
                            return `<a href="${matchedField.value}" target="_blank">
                                        <img src="${matchedField.value}" class="img-thumbnail submissions-thumbnail" />
                                    </a>`;
                        } else {
                            return `<a href="${matchedField.value}" target="_blank" class="btn btn-xs btn-outline-primary py-0 px-2 font-monospace small">Download</a>`;
                        }
                    }
                    return matchedField.value;
                }
            });
        });

        // 3. Append static Actions column
        headerRow.append('<th>Actions</th>');
        columnsConfig.push({
            data: 'id',
            orderable: false,
            searchable: false,
            className: 'align-middle text-center',
            render: function (data, type, row) {
                return `
                    <a href="/Claim/EditForm/${data}" class="btn btn-sm btn-primary me-1">Edit</a>
                    <button type="button" class="btn btn-sm btn-danger btn-delete" data-id="${data}">Delete</button>
                `;
            }
        });

        // 4. Initialize dynamic DataTable
        const table = $('#submissionsTable').DataTable({
            ajax: {
                url: '/Claim/GetSubmissionsJson',
                type: 'GET'
            },
            columns: columnsConfig,
            order: [[0, 'desc']]
        });

        // 5. AJAX Delete Logic
        $('#submissionsTable tbody').on('click', '.btn-delete', function () {
            const id = $(this).data('id');
            if (confirm("Are you sure you want to delete this submission permanently? This deletes all uploaded files associated with it.")) {
                $.ajax({
                    url: `/Claim/DeleteSubmission/${id}`,
                    type: 'POST',
                    success: function (result) {
                        if (result.success) {
                            table.ajax.reload(null, false); // Reloads data without resetting search/page state
                        } else {
                            alert(result.message || "Failed to delete submission.");
                        }
                    },
                    error: function () {
                        alert("An error occurred during deletion.");
                    }
                });
            }
        });
    });
});