'use client';

import { useEffect, useState } from 'react';
import { api } from '@/lib/api';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Users, Shield, Headset, Wrench } from 'lucide-react';

interface Employee {
  id: number;
  email: string;
  fullName: string;
  role: string;
  isActive: boolean;
  lastLoginAt: string | null;
  createdAt: string;
}

const roleConfig: Record<string, { label: string; icon: typeof Shield; color: string; bg: string }> = {
  superadmin: { label: 'Super Admin', icon: Shield, color: 'text-indigo-300', bg: 'bg-indigo-950' },
  support: { label: 'Soporte', icon: Headset, color: 'text-cyan-300', bg: 'bg-cyan-950' },
  ops: { label: 'Operaciones', icon: Wrench, color: 'text-amber-300', bg: 'bg-amber-950' },
};

export default function EmployeesPage() {
  const [employees, setEmployees] = useState<Employee[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api
      .get<Employee[]>('/v1/backoffice/employees')
      .then(setEmployees)
      .catch(console.error)
      .finally(() => setLoading(false));
  }, []);

  if (loading) {
    return (
      <div className="space-y-6">
        <h1 className="text-2xl font-bold text-white">Empleados</h1>
        <div className="animate-pulse space-y-3">
          {Array.from({ length: 3 }).map((_, i) => (
            <div key={i} className="h-16 bg-slate-800 rounded-lg" />
          ))}
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-white">Empleados de Plataforma</h1>
          <p className="text-sm text-slate-400 mt-1">
            {employees.length} empleado{employees.length !== 1 ? 's' : ''} registrado{employees.length !== 1 ? 's' : ''}
          </p>
        </div>
      </div>

      {/* Stats by role */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        {Object.entries(roleConfig).map(([role, config]) => {
          const count = employees.filter((e) => e.role === role).length;
          const Icon = config.icon;
          return (
            <Card key={role} className="bg-slate-900 border-slate-800">
              <CardContent className="p-4 flex items-center gap-3">
                <div className={`flex h-10 w-10 items-center justify-center rounded-lg ${config.bg}`}>
                  <Icon className={`h-5 w-5 ${config.color}`} />
                </div>
                <div>
                  <p className="text-xs text-slate-500">{config.label}</p>
                  <p className="text-xl font-bold text-white">{count}</p>
                </div>
              </CardContent>
            </Card>
          );
        })}
      </div>

      {/* Table */}
      <Card className="bg-slate-900 border-slate-800 overflow-hidden">
        <CardHeader>
          <CardTitle className="text-white text-lg flex items-center gap-2">
            <Users className="h-5 w-5 text-indigo-400" /> Equipo
          </CardTitle>
        </CardHeader>
        <CardContent className="p-0">
          {employees.length === 0 ? (
            <div className="p-8 text-center text-slate-500">
              <Users className="h-10 w-10 mx-auto mb-2 opacity-50" />
              <p>No hay empleados registrados</p>
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-slate-800 bg-slate-800/50">
                    <th className="px-4 py-3 text-left text-xs font-medium text-slate-400 uppercase">Nombre</th>
                    <th className="px-4 py-3 text-left text-xs font-medium text-slate-400 uppercase">Email</th>
                    <th className="px-4 py-3 text-center text-xs font-medium text-slate-400 uppercase">Rol</th>
                    <th className="px-4 py-3 text-center text-xs font-medium text-slate-400 uppercase">Estado</th>
                    <th className="px-4 py-3 text-left text-xs font-medium text-slate-400 uppercase">Último login</th>
                    <th className="px-4 py-3 text-left text-xs font-medium text-slate-400 uppercase">Creado</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-800">
                  {employees.map((e) => {
                    const rc = roleConfig[e.role] ?? roleConfig.ops;
                    return (
                      <tr key={e.id} className="hover:bg-slate-800/50 transition-colors">
                        <td className="px-4 py-3 text-slate-200 font-medium">{e.fullName}</td>
                        <td className="px-4 py-3 text-slate-400">{e.email}</td>
                        <td className="px-4 py-3 text-center">
                          <span className={`inline-flex items-center rounded-md px-2 py-0.5 text-xs font-medium ${rc.bg} ${rc.color}`}>
                            {rc.label}
                          </span>
                        </td>
                        <td className="px-4 py-3 text-center">
                          <span
                            className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${
                              e.isActive ? 'bg-emerald-950 text-emerald-300' : 'bg-red-950 text-red-300'
                            }`}
                          >
                            {e.isActive ? 'Activo' : 'Inactivo'}
                          </span>
                        </td>
                        <td className="px-4 py-3 text-slate-500 text-xs">
                          {e.lastLoginAt ? new Date(e.lastLoginAt).toLocaleString('es-PE') : 'Nunca'}
                        </td>
                        <td className="px-4 py-3 text-slate-500 text-xs">
                          {new Date(e.createdAt).toLocaleDateString('es-PE')}
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
