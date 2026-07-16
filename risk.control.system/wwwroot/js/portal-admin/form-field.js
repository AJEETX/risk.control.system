// Start the index counting from how many fields the server sent down
let fieldIndex = document.querySelectorAll('#fieldsContainer tr').length;

document.getElementById('btnAddField').addEventListener('click', addFieldRow);

const container = document.getElementById('fieldsContainer');

container.addEventListener('click', function (event) {
    if (event.target && event.target.classList.contains('btn-remove-row')) {
        const indexToRemove = event.target.getAttribute('data-index');
        removeFieldRow(indexToRemove);
    }
});

container.addEventListener('change', function (event) {
    if (event.target && event.target.classList.contains('field-type-select')) {
        const index = event.target.getAttribute('data-index');
        toggleDropdownInput(event.target, index);
    }
});

function addFieldRow() {
    const html = `
                <tr id="row_${fieldIndex}">
                    <td>
                        <input name="fields[${fieldIndex}].Label" class="form-control" placeholder="Label" required />
                    </td>
                    <td>
                        <select name="fields[${fieldIndex}].FieldType" class="form-control field-type-select" data-index="${fieldIndex}">
                            <option value="text">Text</option>
                            <option value="number">Number</option>
                            <option value="date">Date</option>
                            <option value="file">File Upload</option>
                            <option value="dropdown">Dropdown</option>
                        </select>
                    </td>
                    <td>
                        <input id="options_${fieldIndex}" name="fields[${fieldIndex}].DropdownOptions" class="form-control d-none" placeholder="e.g. Option1, Option2" />
                    </td>
                    <td>
                        <input type="checkbox" name="fields[${fieldIndex}].IsRequired" value="true" class="form-check-input" />
                    </td>
                    <td>
                        <button type="button" class="btn btn-danger btn-sm btn-remove-row" data-index="${fieldIndex}">Remove</button>
                    </td>
                </tr>
            `;
    container.insertAdjacentHTML('beforeend', html);
    fieldIndex++;
}

function removeFieldRow(index) {
    const rowToRemove = document.getElementById(`row_${index}`);
    if (rowToRemove) {
        rowToRemove.remove();
        reIndexRows();
    }
}

function toggleDropdownInput(selectElement, index) {
    const optionsInput = document.getElementById(`options_${index}`);
    if (selectElement.value === 'dropdown') {
        optionsInput.classList.remove('d-none');
    } else {
        optionsInput.classList.add('d-none');
        optionsInput.value = '';
    }
}

function reIndexRows() {
    const rows = document.querySelectorAll('#fieldsContainer tr');
    rows.forEach((row, index) => {
        row.id = `row_${index}`;
        row.querySelector('input[name*=".Label"]').name = `fields[${index}].Label`;

        const select = row.querySelector('.field-type-select');
        select.name = `fields[${index}].FieldType`;
        select.setAttribute('data-index', index);

        const optInput = row.querySelector('input[name*=".DropdownOptions"]');
        optInput.name = `fields[${index}].DropdownOptions`;
        optInput.id = `options_${index}`;

        const reqCheckbox = row.querySelector('input[type="checkbox"]');
        reqCheckbox.name = `fields[${index}].IsRequired`;

        const removeBtn = row.querySelector('.btn-remove-row');
        removeBtn.setAttribute('data-index', index);
    });
    fieldIndex = rows.length;
}