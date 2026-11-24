# Arquivos de Amostra para Demonstração de Importação CSV

Estes arquivos foram preparados para testar e demonstrar o funcionamento do **NeoAgenda** tanto via interface gráfica quanto via API Swagger.

---

## 1. `contatos_validos.csv`
Contém 7 contatos válidos com formatações variadas de telefone:
- Máscara com parênteses e hífen: `(11) 98765-1122`
- Dígitos puros sem formatação: `21998884433`
- Prefixo DDI internacional com espaços: `+55 31 99111-5566`
- Prefixo de longa distância com zero e fixo: `041 3344 7788`
- Celulares com pontuação e ponto no 9º dígito: `+55 (81) 9.9555-6677`
- Formato DDI direto: `5551984443322`

**Resultado Esperado:** 7 contatos importados com sucesso, 0 erros.

---

## 2. `contatos_com_erros.csv`
Contém 7 linhas propositalmente desenhadas para demonstrar a **tolerância a falhas linha a linha (RN02)**:
- **Linha 2 (Válida):** Mariana Santos (importada com sucesso).
- **Linha 3 (Inválida):** E-mail sem formato RFC (`sem arroba`).
- **Linha 4 (Inválida):** Nome em branco.
- **Linha 5 (Inválida):** Telefone curto demais (`12345`).
- **Linha 6 (Inválida):** DDD inexistente no Brasil (`00`).
- **Linha 7 (Inválida):** Duplicidade intra-arquivo (mesmo número da Mariana da linha 2 em formato diferente).
- **Linha 8 (Válida):** Paulo Sergio (importado com sucesso).

**Resultado Esperado:** 2 contatos importados com sucesso, 5 linhas com inconsistências detalhadas no relatório visual e exportáveis em CSV.
