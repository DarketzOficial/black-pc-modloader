# Pacotes recebidos

Coloque aqui arquivos `.bmod` prontos para revisão, com nome
`id-versão.bmod` (exemplo: `texto-ptbr-1.2.0.bmod`).

O GitHub Actions roda `ferramentas/validar_pacote.py` em todos os pacotes desta
pasta. Um pacote inválido faz o CI falhar, então não faça merge até corrigir.

O mantenedor importa os pacotes com o `modkit` em uma instalação limpa;
veja `comunidade/CHECKLIST-MANTENEDOR.md`.
