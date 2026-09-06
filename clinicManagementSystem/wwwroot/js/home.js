document.addEventListener("DOMContentLoaded", function () {
    console.log("Home JS Loaded Successfully");

    const navLinks = document.querySelectorAll('.smooth-scroll');
    navLinks.forEach(link => {
        link.addEventListener('click', function (e) {
            const targetId = this.getAttribute('href');
            if (targetId && targetId.includes('#')) {
                const idOnly = targetId.substring(targetId.indexOf('#'));
                const targetElement = document.querySelector(idOnly);
                if (targetElement) {
                    e.preventDefault();

                    const navbarCollapse = document.getElementById('navbarContent');
                    if (navbarCollapse && navbarCollapse.classList.contains('show')) {
                        const bsCollapse = bootstrap.Collapse.getInstance(navbarCollapse);
                        if (bsCollapse) bsCollapse.hide();
                    }

                    targetElement.scrollIntoView({
                        behavior: 'smooth',
                        block: 'start'
                    });
                }
            }
        });
    });

   
    const toggleBtn = document.getElementById('patientPortalToggle');
    const floatingMenu = document.getElementById('patientFloatingMenu');
    const closeBtn = document.getElementById('closeFloatingMenu');

    if (toggleBtn && floatingMenu) {
        toggleBtn.addEventListener('click', function (e) {
            e.stopPropagation();
            floatingMenu.classList.toggle('show');
        });

        if (closeBtn) {
            closeBtn.addEventListener('click', function () {
                floatingMenu.classList.remove('show');
            });
        }

        document.addEventListener('click', function (e) {
            if (!floatingMenu.contains(e.target) && !toggleBtn.contains(e.target)) {
                floatingMenu.classList.remove('show');
            }
        });
    }
});