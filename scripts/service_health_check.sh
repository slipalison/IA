#!/bin/bash

echo "🔍 Teste rápido dos serviços..."

echo "📦 Containers rodando:"
docker compose ps

echo ""
echo "🔍 Testando serviços principais:"

# Ollama (esse deve funcionar)
echo -n "Ollama: "
curl -s -f http://localhost:11434/api/tags >/dev/null 2>&1 && echo "✅" || echo "❌"

# PostgreSQL
echo -n "PostgreSQL: "
docker exec devops-postgres pg_isready -U devopsai >/dev/null 2>&1 && echo "✅" || echo "❌"

# Redis
echo -n "Redis: "
docker exec devops-redis redis-cli ping >/dev/null 2>&1 && echo "✅" || echo "❌"

# Chroma - vamos testar vários endpoints
echo -n "Chroma: "
if curl -s http://localhost:8000/api/v1/heartbeat >/dev/null 2>&1; then
    echo "✅ (v1)"
elif curl -s http://localhost:8000/api/v2/heartbeat >/dev/null 2>&1; then
    echo "✅ (v2)"
elif curl -s http://localhost:8000/heartbeat >/dev/null 2>&1; then
    echo "✅ (raiz)"
else
    echo "❌ - Logs:"
    docker compose logs chroma --tail=3
fi

echo ""
echo "🚀 Pelo menos Ollama, PostgreSQL e Redis devem estar funcionando!"



curl -X POST http://localhost:8000/api/v2/collections \
  -H "Content-Type: application/json" \
  -d '{
    "name": "test_collection",
    "metadata": {"description": "Teste inicial"}
  }'

# Listar coleções
curl -X GET http://localhost:8000/api/v2/collections
