// Graph Explorer using vis-network library
let network = null;
let nodes = null;
let edges = null;
let dotNetReference = null;

// Color scheme for different node types
const nodeColors = {
    'GoodWork': '#FF6B6B',
    'Organization': '#4ECDC4',
    'User': '#45B7D1',
    'Category': '#FFA07A',
    'Location': '#98D8C8',
    'Contact': '#F7DC6F',
    'Skill': '#BB8FCE',
    'Tag': '#85C1E2',
    'SubCategory': '#F39C12',
    'Default': '#95A5A6'
};

window.initializeGraph = function () {
    const container = document.getElementById('graph-container');
    if (!container) {
        console.error('Graph container not found');
        return;
    }

    // Initialize empty data
    nodes = new vis.DataSet([]);
    edges = new vis.DataSet([]);

    const data = {
        nodes: nodes,
        edges: edges
    };

    const options = {
        nodes: {
            shape: 'dot',
            size: 20,
            font: {
                size: 14,
                color: '#ffffff'
            },
            borderWidth: 2,
            borderWidthSelected: 4,
            shadow: true
        },
        edges: {
            width: 2,
            color: {
                color: '#848484',
                highlight: '#2B7CE9',
                hover: '#2B7CE9'
            },
            arrows: {
                to: {
                    enabled: true,
                    scaleFactor: 0.5
                }
            },
            smooth: {
                type: 'cubicBezier',
                forceDirection: 'horizontal',
                roundness: 0.4
            },
            font: {
                size: 11,
                align: 'middle',
                background: 'white',
                strokeWidth: 2,
                strokeColor: 'white'
            }
        },
        physics: {
            enabled: true,
            barnesHut: {
                gravitationalConstant: -2000,
                centralGravity: 0.3,
                springLength: 150,
                springConstant: 0.04,
                damping: 0.09,
                avoidOverlap: 0.1
            },
            stabilization: {
                iterations: 200,
                updateInterval: 25
            }
        },
        interaction: {
            hover: true,
            tooltipDelay: 200,
            navigationButtons: true,
            keyboard: true
        },
        layout: {
            improvedLayout: true,
            hierarchical: false
        }
    };

    network = new vis.Network(container, data, options);

    // Event handlers
    network.on('click', function (params) {
        if (params.nodes.length > 0) {
            const nodeId = params.nodes[0];
            if (dotNetReference) {
                dotNetReference.invokeMethodAsync('SelectNode', nodeId);
            }
        } else if (params.edges.length > 0) {
            const edgeId = params.edges[0];
            if (dotNetReference) {
                dotNetReference.invokeMethodAsync('SelectEdge', edgeId);
            }
        }
    });

    network.on('doubleClick', function (params) {
        if (params.nodes.length > 0) {
            const nodeId = params.nodes[0];
            if (dotNetReference) {
                dotNetReference.invokeMethodAsync('DoubleClickNode', nodeId);
            }
        }
    });

    network.on('stabilizationIterationsDone', function () {
        network.setOptions({ physics: false });
    });

    console.log('Graph initialized');
};

window.renderGraph = function (nodesJson, edgesJson, dotNetRef) {
    if (!network) {
        console.error('Network not initialized');
        return;
    }

    dotNetReference = dotNetRef;

    try {
        const nodesData = JSON.parse(nodesJson);
        const edgesData = JSON.parse(edgesJson);

        console.log(`Rendering graph with ${nodesData.length} nodes and ${edgesData.length} edges`);

        // Clear existing data
        nodes.clear();
        edges.clear();

        if (nodesData.length === 0) {
            console.warn('No nodes to render');
            return;
        }

        // Process and add nodes
        const processedNodes = nodesData.map(node => ({
            id: node.Id,
            label: node.Title.length > 30 ? node.Title.substring(0, 27) + '...' : node.Title,
            title: `${node.Type}: ${node.Title}`,
            color: {
                background: nodeColors[node.Type] || nodeColors['Default'],
                border: '#2B7CE9',
                highlight: {
                    background: nodeColors[node.Type] || nodeColors['Default'],
                    border: '#2B7CE9'
                }
            },
            group: node.Type
        }));

        // Process and add edges
        const processedEdges = edgesData.map(edge => ({
            id: edge.Id,
            from: edge.From,
            to: edge.To,
            label: edge.Label,
            title: edge.Type,
            arrows: {
                to: {
                    enabled: true,
                    scaleFactor: 0.5
                }
            }
        }));

        nodes.add(processedNodes);
        edges.add(processedEdges);

        console.log(`Added ${processedNodes.length} nodes and ${processedEdges.length} edges to graph`);

        // Enable physics temporarily for layout
        network.setOptions({ physics: { enabled: true } });

        // Fit to view after a short delay to let physics stabilize a bit
        setTimeout(() => {
            network.fit({
                animation: {
                    duration: 1000,
                    easingFunction: 'easeInOutQuad'
                }
            });
            console.log('Graph fitted to view');
        }, 100);

        console.log(`Successfully rendered graph`);
    } catch (error) {
        console.error('Error rendering graph:', error);
        console.error('Nodes JSON:', nodesJson);
        console.error('Edges JSON:', edgesJson);
    }
};

window.addGraphData = function (nodesJson, edgesJson) {
    if (!network) {
        console.error('Network not initialized');
        return;
    }

    try {
        const nodesData = JSON.parse(nodesJson);
        const edgesData = JSON.parse(edgesJson);

        // Process new nodes
        const newNodes = nodesData
            .filter(node => !nodes.get(node.Id))
            .map(node => ({
                id: node.Id,
                label: node.Title.length > 30 ? node.Title.substring(0, 27) + '...' : node.Title,
                title: `${node.Type}: ${node.Title}`,
                color: {
                    background: nodeColors[node.Type] || nodeColors['Default'],
                    border: '#2B7CE9',
                    highlight: {
                        background: nodeColors[node.Type] || nodeColors['Default'],
                        border: '#2B7CE9'
                    }
                },
                group: node.Type
            }));

        // Process new edges
        const newEdges = edgesData
            .filter(edge => !edges.get(edge.Id))
            .map(edge => ({
                id: edge.Id,
                from: edge.From,
                to: edge.To,
                label: edge.Label,
                title: edge.Type
            }));

        if (newNodes.length > 0) {
            nodes.add(newNodes);
        }

        if (newEdges.length > 0) {
            edges.add(newEdges);
        }

        // Enable physics temporarily for layout
        network.setOptions({ physics: { enabled: true } });

        console.log(`Added ${newNodes.length} nodes and ${newEdges.length} edges`);
    } catch (error) {
        console.error('Error adding graph data:', error);
    }
};

window.focusNode = function (nodeId) {
    if (!network) {
        console.error('Network not initialized');
        return;
    }

    try {
        network.focus(nodeId, {
            scale: 1.5,
            animation: {
                duration: 1000,
                easingFunction: 'easeInOutQuad'
            }
        });

        network.selectNodes([nodeId]);
    } catch (error) {
        console.error('Error focusing node:', error);
    }
};

window.clearGraph = function () {
    if (nodes && edges) {
        nodes.clear();
        edges.clear();
    }
};
