import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'
import CadastroAlunos from './pages/CadastrarAlunos.tsx'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <CadastroAlunos />
  </StrictMode>,
)
