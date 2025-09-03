Processador Assíncrono de Arquivos de Texto (.NET 8, C#)
Aplicação Console em C# (.NET 8) que permite ao usuário selecionar um diretório (ou arquivo .zip) contendo arquivos .txt e processa cada um de forma assíncrona e paralela, realizando a contagem de linhas e palavras.
O resultado final é consolidado em um relatório relatorio.txt dentro da pasta export.

Saída
Durante a execução o console exibe o progresso, por exemplo:

Processando: arquivo001.txt ...
Concluído : arquivo001.txt - 18 linha(s), 211 palavra(s).


No final, é gerado o arquivo de relatório em:

./bin/Debug/net8.0/export/relatorio.txt, ou
./bin/Release/net8.0/export/relatorio.txt (em compilação Release)


O conteúdo do relatório segue o formato:

Arquivo - Linhas - Palavras
arquivo1.txt - 100 linhas - 560 palavras
arquivo2.txt - 230 linhas - 1500 palavras
Integrantes
Bruno Venturi Lopes Vieira — RM: 99431
