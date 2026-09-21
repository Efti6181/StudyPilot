(() => {
    "use strict";

    const locationSelect = document.getElementById("eventLocationType");
    const venueField = document.getElementById("eventVenueField");
    const onlineField = document.getElementById("eventOnlineUrlField");
    if (!locationSelect || !venueField || !onlineField) return;

    const venueInput = venueField.querySelector("input");
    const onlineInput = onlineField.querySelector("input");

    const updateLocationFields = () => {
        const value = locationSelect.value;
        const isCampus = value === "0";
        const isOnline = value === "1";
        const isHybrid = value === "2";

        venueField.hidden = isOnline;
        onlineField.hidden = isCampus;
        if (venueInput) venueInput.required = isCampus || isHybrid;
        if (onlineInput) onlineInput.required = isOnline || isHybrid;
    };

    locationSelect.addEventListener("change", updateLocationFields);
    updateLocationFields();
})();
