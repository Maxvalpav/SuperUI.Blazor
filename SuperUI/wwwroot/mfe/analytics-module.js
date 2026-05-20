/**
 * External Micro Frontend Module: Analytics Dashboard
 */

export function mount(container, parameters) {
    const userCount = parameters.userCount || 1250;
    
    container.innerHTML = `
        <div style="background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 24px; border-radius: 16px; color: white; font-family: system-ui, -apple-system, sans-serif;">
            <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px;">
                <h3 style="margin: 0; font-weight: 500;">Аналитика платформы</h3>
                <span style="background: rgba(255,255,255,0.2); padding: 4px 12px; border-radius: 20px; font-size: 12px;">LIVE</span>
            </div>
            
            <div style="display: grid; grid-template-columns: 1fr 1fr 1fr; gap: 16px;">
                <div style="background: rgba(255,255,255,0.1); padding: 16px; border-radius: 12px;">
                    <div style="font-size: 12px; opacity: 0.8; margin-bottom: 4px;">Пользователи</div>
                    <div style="font-size: 24px; font-weight: bold;">${userCount.toLocaleString()}</div>
                </div>
                <div style="background: rgba(255,255,255,0.1); padding: 16px; border-radius: 12px;">
                    <div style="font-size: 12px; opacity: 0.8; margin-bottom: 4px;">Активность</div>
                    <div style="font-size: 24px; font-weight: bold;">84%</div>
                </div>
                <div style="background: rgba(255,255,255,0.1); padding: 16px; border-radius: 12px;">
                    <div style="font-size: 12px; opacity: 0.8; margin-bottom: 4px;">Конверсия</div>
                    <div style="font-size: 24px; font-weight: bold;">3.2%</div>
                </div>
            </div>
            
            <div style="margin-top: 24px; padding-top: 20px; border-top: 1px solid rgba(255,255,255,0.2);">
                <div style="font-size: 14px; margin-bottom: 8px;">Последние события</div>
                <ul style="margin: 0; padding: 0; list-style: none; font-size: 12px; display: flex; flex-direction: column; gap: 8px;">
                    <li style="display: flex; align-items: center; gap: 8px;">
                        <span style="width: 6px; height: 6px; background: #4fd1c5; border-radius: 50%;"></span>
                        Новый заказ #4921 от Ивана П.
                    </li>
                    <li style="display: flex; align-items: center; gap: 8px;">
                        <span style="width: 6px; height: 6px; background: #4fd1c5; border-radius: 50%;"></span>
                        Регистрация нового вендора: SuperTech
                    </li>
                </ul>
            </div>
        </div>
    `;
}

export function unmount(container) {
    container.innerHTML = '';
}
