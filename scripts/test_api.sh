#!/bin/bash

echo "🧪 Testando DevOpsAI API..."

API_URL="http://localhost:5290"

echo ""
echo "1️⃣ Testando status da API..."
curl -s "$API_URL/status" | jq '.' || curl -s "$API_URL/status"

echo ""
echo "2️⃣ Testando health checks..."
curl -s "$API_URL/health/detailed" | jq '.' || curl -s "$API_URL/health/detailed"

echo ""
echo "3️⃣ Testando modelos do Ollama..."
curl -s "$API_URL/api/ollama/models" | jq '.' || curl -s "$API_URL/api/ollama/models"

echo ""
echo "4️⃣ Testando chat simples..."
curl -X POST "$API_URL/api/ollama/chat" \
     -H "Content-Type: application/json" \
     -d '{"message":"Hello! Como você pode ajudar com DevOps?"}' \
     | jq '.' || echo "Erro no chat"

echo ""
echo "5️⃣ Testando análise de logs..."
curl -X POST "$API_URL/api/devops/analyze-logs" \
     -H "Content-Type: application/json" \
     -d '{"logContent":"2024-06-02 10:30:15 ERROR: Connection timeout to database\n2024-06-02 10:30:16 WARN: Retrying connection...\n2024-06-02 10:30:20 ERROR: Max retries exceeded"}' \
     | jq '.' || echo "Erro na análise de logs"

echo ""
echo "6️⃣ Testando geração de script..."
curl -X POST "$API_URL/api/devops/generate-script" \
     -H "Content-Type: application/json" \
     -d '{"description":"Backup automático de banco PostgreSQL","scriptType":"bash"}' \
     | jq '.' || echo "Erro na geração de script"

echo ""
echo "✅ Testes concluídos!"



echo "🧪 Testando Sistema RAG de Documentação..."

API_URL="http://localhost:5290"

echo ""
echo "1️⃣ Inicializando base de documentação..."
curl -X POST "$API_URL/api/documentation/initialize" \
     -H "Content-Type: application/json" \
     | jq '.' || echo "Erro na inicialização"

echo ""
echo "2️⃣ Perguntando sobre Docker..."
curl -X POST "$API_URL/api/documentation/ask" \
     -H "Content-Type: application/json" \
     -d '{"question":"Como criar um Dockerfile para uma aplicação Node.js?","maxResults":2}' \
     | jq '.' || echo "Erro na consulta"

echo ""
echo "3️⃣ Perguntando sobre Kubernetes..."
curl -X POST "$API_URL/api/documentation/ask" \
     -H "Content-Type: application/json" \
     -d '{"question":"Quais são os componentes principais do Kubernetes?","maxResults":3}' \
     | jq '.' || echo "Erro na consulta"

echo ""
echo "4️⃣ Adicionando documento personalizado..."
curl -X POST "$API_URL/api/documentation/add-document" \
     -H "Content-Type: application/json" \
     -d '{"title":"Git Workflow","content":"Git Flow é um modelo de branching: main/master (produção), develop (desenvolvimento), feature/* (funcionalidades), release/* (preparação), hotfix/* (correções urgentes). Comandos: git flow init, git flow feature start, git flow feature finish.","category":"git"}' \
     | jq '.' || echo "Erro ao adicionar"

echo ""
echo "5️⃣ Perguntando sobre Git que acabamos de adicionar..."
curl -X POST "$API_URL/api/documentation/ask" \
     -H "Content-Type: application/json" \
     -d '{"question":"Como funciona o Git Flow?","maxResults":2}' \
     | jq '.' || echo "Erro na consulta"

echo ""
echo "✅ Testes RAG concluídos!"


# Verificar endpoints disponíveis
curl -s "http://localhost:8000/api/v2/collections" | jq .

# Testar criar coleção com API v1
curl -X POST "http://localhost:8000/api/v2/collections" \
     -H "Content-Type: application/json" \
     -d '{
       "name": "devops-docs",
       "metadata": {"description": "Documentação DevOps"}
     }'
