import React from 'react';
import ReactDOM from 'react-dom/client';
import App from './App';

/**
 * Ponto de entrada do SPA.
 * 
 * Sistema de Folha de Pagamento - Front-End
 * 
 * IMPORTANTE:
 * - Este front-end consome exclusivamente a API
 * - Nenhuma regra de negócio (INSS, IRRF, FGTS) existe aqui
 * - Respeita RBAC definido na API
 */

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>,
);
