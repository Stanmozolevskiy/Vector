import { useMemo } from 'react';

interface ChartData {
  label: string;
  value: number;
  color?: string;
}

interface ProgressChartProps {
  data: ChartData[];
  type?: 'bar' | 'pie' | 'line';
  title?: string;
  height?: number;
}

export const ProgressChart = ({ data, type = 'bar', title, height = 200 }: ProgressChartProps) => {
  const maxValue = useMemo(() => {
    if (data.length === 0) return 1;
    return Math.max(...data.map(d => d.value), 1);
  }, [data]);

  const totalValue = useMemo(() => {
    return data.reduce((sum, d) => sum + d.value, 0);
  }, [data]);

  const defaultColors = [
    '#3b82f6', // blue
    '#10b981', // green
    '#f59e0b', // amber
    '#ef4444', // red
    '#8b5cf6', // purple
    '#ec4899', // pink
    '#06b6d4', // cyan
    '#84cc16', // lime
  ];

  const getColor = (index: number, color?: string) => {
    return color || defaultColors[index % defaultColors.length];
  };

  if (data.length === 0) {
    return (
      <div className="chart-container" style={{ height: `${height}px`, display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
        <p style={{ color: '#6b7280' }}>No data available</p>
      </div>
    );
  }

  return (
    <div className="chart-container" style={{ height: `${height}px` }}>
      {title && (
        <h3 style={{ fontSize: '1rem', fontWeight: 600, marginBottom: '1rem', color: '#111827' }}>
          {title}
        </h3>
      )}
      
      {type === 'bar' && (
        <div className="bar-chart" style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem', height: '100%' }}>
          {data.map((item, index) => (
            <div key={item.label} style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
              <div style={{ minWidth: '100px', fontSize: '0.875rem', color: '#374151' }}>
                {item.label}
              </div>
              <div style={{ flex: 1, position: 'relative' }}>
                <div
                  style={{
                    width: `${(item.value / maxValue) * 100}%`,
                    height: '24px',
                    backgroundColor: getColor(index, item.color),
                    borderRadius: '4px',
                    transition: 'width 0.3s ease',
                    display: 'flex',
                    alignItems: 'center',
                    paddingLeft: '8px',
                    color: '#fff',
                    fontSize: '0.75rem',
                    fontWeight: 500,
                  }}
                >
                  {item.value > 0 && item.value}
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      {type === 'pie' && (
        <div className="pie-chart" style={{ position: 'relative', display: 'flex', alignItems: 'center', justifyContent: 'center', height: '100%' }}>
          <svg width={height} height={height} viewBox="0 0 200 200" style={{ transform: 'rotate(-90deg)' }}>
            {data.reduce((acc, item, index) => {
              const percentage = (item.value / totalValue) * 100;
              const strokeDasharray = `${percentage} ${100 - percentage}`;
              const strokeDashoffset = acc.offset;
              const color = getColor(index, item.color);
              
              acc.elements.push(
                <circle
                  key={item.label}
                  cx="100"
                  cy="100"
                  r="80"
                  fill="none"
                  stroke={color}
                  strokeWidth="40"
                  strokeDasharray={strokeDasharray}
                  strokeDashoffset={strokeDashoffset}
                  style={{ transition: 'all 0.3s ease' }}
                />
              );
              
              acc.offset -= percentage;
              return acc;
            }, { elements: [] as React.ReactElement[], offset: 0 }).elements}
          </svg>
          <div style={{ position: 'absolute', textAlign: 'center', pointerEvents: 'none' }}>
            <div style={{ fontSize: '1.5rem', fontWeight: 600, color: '#111827' }}>
              {totalValue}
            </div>
            <div style={{ fontSize: '0.875rem', color: '#6b7280' }}>Total</div>
          </div>
        </div>
      )}

      {type === 'line' && (
        <div className="line-chart" style={{ position: 'relative', height: '100%', padding: '1rem 0' }}>
          <svg width="100%" height={height - 40} style={{ overflow: 'visible' }}>
            <polyline
              points={data.map((item, index) => {
                const x = (index / (data.length - 1 || 1)) * 100;
                const y = 100 - (item.value / maxValue) * 100;
                return `${x}%,${y}%`;
              }).join(' ')}
              fill="none"
              stroke="#3b82f6"
              strokeWidth="2"
              style={{ transition: 'all 0.3s ease' }}
            />
            {data.map((item, index) => {
              const x = (index / (data.length - 1 || 1)) * 100;
              const y = 100 - (item.value / maxValue) * 100;
              return (
                <circle
                  key={item.label}
                  cx={`${x}%`}
                  cy={`${y}%`}
                  r="4"
                  fill="#3b82f6"
                  style={{ transition: 'all 0.3s ease' }}
                />
              );
            })}
          </svg>
        </div>
      )}
    </div>
  );
};

