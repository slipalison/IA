-- infrastructure/postgres/init.sql
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Tabela para repositórios
CREATE TABLE repositories (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(255) NOT NULL,
    url VARCHAR(500) NOT NULL,
    last_indexed TIMESTAMP,
    status VARCHAR(50) DEFAULT 'pending',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Tabela para pull requests
CREATE TABLE pull_requests (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    repository_id UUID REFERENCES repositories(id),
    pr_number INTEGER NOT NULL,
    title VARCHAR(500),
    description TEXT,
    author VARCHAR(255),
    status VARCHAR(50),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    analyzed_at TIMESTAMP
);

-- Tabela para métricas de análise
CREATE TABLE code_analysis (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    pull_request_id UUID REFERENCES pull_requests(id),
    analysis_type VARCHAR(100),
    score DECIMAL(5,2),
    issues JSONB,
    recommendations TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Índices para performance
CREATE INDEX idx_repositories_status ON repositories(status);
CREATE INDEX idx_pull_requests_repo ON pull_requests(repository_id);
CREATE INDEX idx_analysis_pr ON code_analysis(pull_request_id);