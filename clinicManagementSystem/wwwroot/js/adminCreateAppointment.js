document.addEventListener("DOMContentLoaded", function () {
    var doctorSelect = document.getElementById('DoctorId');
    var scheduleSelect = document.getElementById('DoctorScheduleId');
    var timeSlotSelect = document.getElementById('AppointmentTime');
    var dateInput = document.getElementById('AppointmentDate');

    if (doctorSelect) {
        doctorSelect.addEventListener('change', function () {
            var doctorId = this.value;

            scheduleSelect.innerHTML = '<option value="">-- Select Shift --</option>';
            timeSlotSelect.innerHTML = '<option value="">-- Select Time Slot --</option>';

            if (!doctorId || doctorId === "0") return;

            fetch(`/Admin/Appointments/GetDoctorSchedules?doctorId=${doctorId}`)
                .then(res => {
                    if (!res.ok) throw new Error('Network response was not ok');
                    return res.json();
                })
                .then(data => {
                    console.log("Schedules received:", data);
                    if (data.length === 0) {
                        alert("There's no shifts avliable for selected doctor.");
                        return;
                    }
                    data.forEach(item => {
                        var option = new Option(`${item.dayOfWeek} (${item.timeText})`, item.scheduleId);
                        scheduleSelect.add(option);
                    });
                })
                .catch(err => console.error("Error fetching schedules:", err));
        });
    }

    if (dateInput) dateInput.addEventListener('change', loadSlots);
    if (scheduleSelect) scheduleSelect.addEventListener('change', loadSlots);

    function loadSlots() {
        var doctorId = doctorSelect.value;
        var scheduleId = scheduleSelect.value;
        var date = dateInput.value;
        timeSlotSelect.innerHTML = '<option value="">-- Select Time Slot --</option>';

        if (!doctorId || !scheduleId || !date) return;

        fetch(`/Admin/Appointments/GetAvailableSlots?doctorId=${doctorId}&scheduleId=${scheduleId}&date=${date}`)
            .then(res => res.json())
            .then(data => {
                data.forEach(slot => {
                    var option = new Option(slot.text, slot.value);
                    if (slot.disabled) option.disabled = true;
                    timeSlotSelect.add(option);
                });
            })
            .catch(err => console.error("Error fetching slots:", err));
    }
});