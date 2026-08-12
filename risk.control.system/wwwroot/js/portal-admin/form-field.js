let fieldIndex = 0;
let currentActiveSection = 'Policy';

document.addEventListener('DOMContentLoaded', function () {
    // 1. Selectors
    const formTypeSelector = document.getElementById('formTypeSelector');
    const companySelector = document.getElementById('companySelector');
    const btnAddField = document.getElementById('btnAddField');
    const container = document.getElementById('fieldsContainer');

    // Sync initial fieldIndex from rendered rows
    if (container) {
        fieldIndex = container.querySelectorAll('tr').length;
    }

    // 2. Filter redirect synchronization
    function reloadPageWithFilters() {
        const companyId = companySelector ? companySelector.value : '';
        const formType = formTypeSelector ? formTypeSelector.value : '';
        if (companyId && formType) {
            window.location.href = `?companyId=${companyId}&type=${formType}`;
        }
        // If either selection is cleared/unselected, reload to clear the form fields section
        else {
            window.location.href = window.location.pathname;
        }
    }
    function handleFilterChange() {
        const companyId = companySelector ? companySelector.value : '';
        const formType = formTypeSelector ? formTypeSelector.value : '';

        // ONLY reload if BOTH dropdowns have selected values
        if (companyId && formType) {
            reloadPageWithFilters();
        }
    }

    if (formTypeSelector) {
        formTypeSelector.addEventListener('change', handleFilterChange);
    }

    if (companySelector) {
        companySelector.addEventListener('change', handleFilterChange);
    }
    // 3. Tab switching (Only runs if tabs exist on page)
    const tabs = document.querySelectorAll('.section-tab-btn');
    if (tabs.length > 0) {
        const firstActiveTab = document.querySelector('.section-tab-btn.active');
        if (firstActiveTab) {
            currentActiveSection = firstActiveTab.getAttribute('data-section');
        }

        tabs.forEach(tab => {
            tab.addEventListener('click', function (e) {
                e.preventDefault();
                tabs.forEach(t => t.classList.remove('active'));
                this.classList.add('active');

                currentActiveSection = this.getAttribute('data-section');
                filterRowsBySection();
            });
        });

        filterRowsBySection();
    }

    // 4. Safe Event Listeners for Dynamic Table Elements
    if (btnAddField) {
        btnAddField.addEventListener('click', addFieldRow);
    }

    if (container) {
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
    }
});

// Helper functions
function filterRowsBySection() {
    const rows = document.querySelectorAll('.field-row');
    rows.forEach(row => {
        const rowSec = row.getAttribute('data-section');
        if (rowSec === currentActiveSection) {
            row.style.display = '';
        } else {
            row.style.display = 'none';
        }
    });
}

function addFieldRow() {
    const container = document.getElementById('fieldsContainer');
    if (!container) return;

    const html = `
        <tr id="row_${fieldIndex}" data-section="${currentActiveSection}" class="field-row">
            <td>
                <input type="hidden" name="Fields[${fieldIndex}].Section" value="${currentActiveSection}" class="row-section-input" />
                <input name="Fields[${fieldIndex}].Label" class="form-control" placeholder="Label" required />
            </td>
            <td>
                <select name="Fields[${fieldIndex}].FieldType" class="form-control field-type-select" data-index="${fieldIndex}">
                    <option value="text">Text</option>
                    <option value="number">Number</option>
                    <option value="date">Date</option>
                    <option value="file">File Upload</option>
                    <option value="dropdown">Dropdown</option>
                    <option value="address">Address Autocomplete</option>
                </select>
            </td>
            <td>
                <input id="options_${fieldIndex}" name="Fields[${fieldIndex}].DropdownOptions" class="form-control d-none" placeholder="e.g. Option1, Option2" />
            </td>
            <td>
                <input type="checkbox" name="Fields[${fieldIndex}].IsRequired" value="true" class="form-check-input" />
            </td>
            <td>
                <button type="button" class="btn btn-danger btn-sm btn-remove-row" data-index="${fieldIndex}">Remove</button>
            </td>
        </tr>
    `;
    container.insertAdjacentHTML('beforeend', html);
    fieldIndex++;
    filterRowsBySection();
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
    if (!optionsInput) return;

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

        row.querySelector('input[name*=".Section"]').name = `Fields[${index}].Section`;
        row.querySelector('input[name*=".Label"]').name = `Fields[${index}].Label`;

        const select = row.querySelector('.field-type-select');
        if (select) {
            select.name = `Fields[${index}].FieldType`;
            select.setAttribute('data-index', index);
        }

        const optInput = row.querySelector('input[name*=".DropdownOptions"]');
        if (optInput) {
            optInput.name = `Fields[${index}].DropdownOptions`;
            optInput.id = `options_${index}`;
        }

        const reqCheckbox = row.querySelector('input[type="checkbox"]');
        if (reqCheckbox) {
            reqCheckbox.name = `Fields[${index}].IsRequired`;
        }

        const removeBtn = row.querySelector('.btn-remove-row');
        if (removeBtn) {
            removeBtn.setAttribute('data-index', index);
        }
    });
    fieldIndex = rows.length;
    filterRowsBySection();
}