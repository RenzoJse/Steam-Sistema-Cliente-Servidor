# Steam — Sistema Cliente-Servidor

**Steam** es un sistema cliente-servidor que simula una plataforma de distribución digital de videojuegos. Permite la creación de cuentas, publicación, compra, modificación y calificación de juegos, garantizando una comunicación eficiente mediante un **protocolo propietario implementado sobre TCP/IP**.

---

## Funcionalidades principales

### Aplicación Cliente
- **Autenticación:** creación, inicio y cierre de sesión de usuarios.  
- **Gestión de juegos:** publicación, edición y eliminación de títulos.  
- **Compra y calificación:** adquisición de juegos disponibles y valoración posterior.  
- **Búsqueda avanzada:** filtrado por criterios como género o plataforma.  

### Aplicación Servidor
- **Atención concurrente:** manejo simultáneo de múltiples clientes.  
- **Protocolo TCP personalizado:** comunicación orientada a caracteres con estructura definida (`HEADER`, `CMD`, `LARGO`, `DATOS`).  
- **Sincronización de Threads:** control de concurrencia y acceso seguro a recursos compartidos.  
- **Gestión de archivos:** almacenamiento y eliminación de carátulas asociadas a los juegos.  
- **Cierre controlado:** finalización ordenada de conexiones activas desde la consola del servidor.  

---

## Aspectos técnicos

- **Lenguaje:** C#  
- **Comunicación:** Sockets TCP/IP  
- **Concurrencia:** manejo de Threads con sincronización.  
- **Streams:** transmisión eficiente de datos e imágenes.  
- **Protocolo:** diseño e implementación propia con tramas estructuradas y control de errores.  
- **Configuración:** parámetros editables sin necesidad de recompilar.  

---

## Arquitectura

El sistema se compone de dos módulos independientes:

1. **Servidor:** gestiona los datos de los juegos, las conexiones activas y la persistencia de información.  
2. **Cliente:** ofrece una interfaz para que los usuarios interactúen con la plataforma de forma remota.

Ambos módulos se comunican mediante un **protocolo TCP propietario**, con mensajes orientados a caracteres y campos de longitud fija para garantizar la correcta interpretación entre procesos.
