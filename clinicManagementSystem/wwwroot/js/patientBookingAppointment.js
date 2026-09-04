document.addEventListener("DOMContentLoaded", function () {
    const dateInput = document.getElementById('appointmentDate');
    const doctorIdInput = document.getElementById('DoctorId');
    const scheduleSelect = document.getElementById('DoctorScheduleId');
    const timeSelect = document.getElementById('appointmentTime');
    const bookingForm = document.getElementById('patientBookingForm');

    const targetDayOfWeek = parseInt(dateInput ? dateInput.getAttribute('data-target-day') : '1');

    if (dateInput) {
        const today = new Date().toISOString().split('T')[0];
        dateInput.setAttribute('min', today);

        dateInput.addEventListener('change', function () {
            validateAndReloadDate();
        });
    }

    if (scheduleSelect) {
        scheduleSelect.addEventListener('change', function () {
            updateShiftAndReload();
        });
    }

    if (bookingForm) {
        bookingForm.addEventListener('submit', function (e) {
            if (!validateForm()) {
                e.preventDefault();
            }
        });
    }

    function updateShiftAndReload() {
        const doctorId = doctorIdInput ? doctorIdInput.value : '';
        const scheduleId = scheduleSelect ? scheduleSelect.value : '';
        const timeStamp = new Date().getTime();

        window.location.href = /Patient/Appointments / Book ? doctorId = ${ doctorId }& scheduleId=${ scheduleId }& _=${ timeStamp };
    }

    function validateAndReloadDate() {
        if (dateInput && dateInput.value) {
            const parts = dateInput.value.split('-');
            const selectedDate = new Date(parts[0], parts[1] - 1, parts[2]);
            const day = selectedDate.getDay();

            if (day !== targetDayOfWeek) {
                alert("The selected date does not match the shift day. Please choose a valid date for this shift.");
                dateInput.value = dateInput.getAttribute('data-default-date') || '';
                return;
            }
        }

        const doctorId = doctorIdInput ? doctorIdInput.value : '';
        const scheduleId = scheduleSelect ? scheduleSelect.value : '';
        const date = dateInput ? dateInput.value : '';
        const timeStamp = new Date().getTime();

        window.location.href = /Patient/Appointments / Book ? doctorId = ${ doctorId }& scheduleId=${ scheduleId }& date=${ date }& _=${ timeStamp };
    }

    function validateForm() {
        if (timeSelect && !timeSelect.value) {
            alert("Please select a time slot.");
            return false;
        }

        if (dateInput && dateInput.value) {
            const parts = dateInput.value.split('-');
            const selectedDate = new Date(parts[0], parts[1] - 1, parts[2]);
            const day = selectedDate.getDay();

            if (day !== targetDayOfWeek) {
                alert("Date day does not match shift day!");
                return false;
            }
        }
        return true;
    }
});