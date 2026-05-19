/**
 * SgTimePicker Clock Interaction
 */
export function initClock(dotNetRef, faceElement) {
    let isDragging = false;

    const handleMove = (e) => {
        if (!isDragging) return;
        
        const rect = faceElement.getBoundingClientRect();
        const centerX = rect.left + rect.width / 2;
        const centerY = rect.top + rect.height / 2;
        
        const clientX = e.type.startsWith('touch') ? e.touches[0].clientX : e.clientX;
        const clientY = e.type.startsWith('touch') ? e.touches[0].clientY : e.clientY;

        const dx = clientX - centerX;
        const dy = clientY - centerY;
        
        let angle = Math.atan2(dy, dx) * (180 / Math.PI) + 90;
        if (angle < 0) angle += 360;
        
        // Local visual update for smoothness
        const hand = faceElement.querySelector('.sgc-clock-hand');
        if (hand) {
            hand.style.transform = `rotate(${angle}deg)`;
            hand.classList.add('sgc-no-transition');
        }

        dotNetRef.invokeMethodAsync('OnClockMove', angle);
    };

    const handleUp = (e) => {
        if (isDragging) {
            isDragging = false;
            
            const hand = faceElement.querySelector('.sgc-clock-hand');
            if (hand) {
                hand.classList.remove('sgc-no-transition');
            }

            dotNetRef.invokeMethodAsync('OnClockUp');
            
            window.removeEventListener('mousemove', handleMove);
            window.removeEventListener('mouseup', handleUp);
            window.removeEventListener('touchmove', handleMove);
            window.removeEventListener('touchend', handleUp);
        }
    };

    const handleDown = (e) => {
        if (e.type === 'mousedown' && e.button !== 0) return;
        
        isDragging = true;
        handleMove(e);
        
        if (e.type === 'touchstart') {
            window.addEventListener('touchmove', handleMove, { passive: false });
            window.addEventListener('touchend', handleUp);
        } else {
            window.addEventListener('mousemove', handleMove);
            window.addEventListener('mouseup', handleUp);
        }
        
        e.preventDefault();
    };

    faceElement.addEventListener('mousedown', handleDown);
    faceElement.addEventListener('touchstart', handleDown, { passive: false });
    
    return {
        dispose: () => {
            faceElement.removeEventListener('mousedown', handleDown);
            faceElement.removeEventListener('touchstart', handleDown);
        }
    };
}
