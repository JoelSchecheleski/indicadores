# Mercado Financeiro - API (NinjaTrader)

Este projeto contém indicadores e estratégias automatizadas para o mercado financeiro, especialmente focado em operações com o índice Nasdaq (NQ) na plataforma NinjaTrader.

## Estrutura do Projeto

O projeto está organizado em duas principais pastas:

### Pasta src/Indicators

#### PivotSignalSimpleDynamic
- **Descrição**: Indicador que identifica pivôs de alta e baixa no gráfico
- **Funcionalidades**:
  - Identifica pivôs de alta quando Low[2] > Low[1] && Low[0] > Low[1]
  - Identifica pivôs de baixa quando High[2] < High[1] && High[0] < High[1]
  - Desenha pontos verdes (compra) e vermelhos (venda) no gráfico
  - Fornece sinais através das propriedades BuySignal e SellSignal

### Pasta src/Strategy

#### PivotStrategyDynamicNQ2
- **Descrição**: Estratégia automatizada que opera baseada nos sinais do PivotSignalSimpleDynamic
- **Características**:
  - Gerenciamento dinâmico de posições
  - Break-even automático após 20 ticks de lucro
  - Trailing stop dinâmico de 40 ticks
  - Stop loss inicial configurável (padrão: 10 ticks)
  - Alvo de lucro de 120 ticks (30 pontos)
  - Proteção de saída na virada da sessão

### Parâmetros Configuráveis da Estratégia
- `Contracts`: Quantidade de contratos por operação
- `InitialStopTicks`: Stop loss inicial em ticks
- `breakEvenTicks`: Distância para ativar break-even (20 ticks)
- `trailingDistanceTicks`: Distância do trailing stop (40 ticks)
- `profitTargetTicks`: Alvo de lucro (120 ticks)

## Requisitos Técnicos

- Plataforma NinjaTrader 8
- .NET Framework
- Conta com acesso ao mercado futuro de NQ (Nasdaq)

## Instalação

1. Copie os arquivos da pasta `compiled` para o diretório de instalação do NinjaTrader
2. Importe os indicadores e estratégias através do Control Center do NinjaTrader
3. Configure os parâmetros desejados antes de iniciar as operações

## Gestão de Risco

- Sempre teste a estratégia em conta simulada antes de usar em conta real
- Monitore o desempenho e ajuste os parâmetros conforme necessário
- Respeite seu plano de risco e tamanho de posição

## Desenvolvimento

O código fonte está disponível na pasta `src` para customizações e melhorias. Para contribuir:
1. Faça um fork do repositório
2. Crie sua feature branch
3. Faça commit das alterações
4. Push para a branch
5. Crie um Pull Request

## Observações

Este projeto é mantido e atualizado regularmente. Sugestões e contribuições são bem-vindas através de issues ou pull requests.
