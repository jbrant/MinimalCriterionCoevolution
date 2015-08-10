using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NavigationEngine
{
    //circle class, does some basic trig
    public class Circle2D
    {
        public Point2D p;
        public double radius;
        public Circle2D() { }
        public Circle2D(Circle2D other)
        {
            p = other.p;
            radius = other.radius;
        }
        public Circle2D(Point2D a, double rad)
        {
            radius = rad;
            p = a;
        }

        //test if circle collides with other circle
        //used in robot collision detection
        public bool collide(Circle2D other)
        {
            double dx = other.p.x - p.x;
            double dy = other.p.y - p.y;
            dx *= dx;
            dy *= dy;
            double rad_sum = other.radius + radius;
            rad_sum *= rad_sum;
            if ((dx + dy) < rad_sum)
                return true;
            return false;
        }
    }

    //point class, does some basic trig
    public class Point2D
    {
        public double x, y;
        public Point2D() { }
        public Point2D(double a, double b)
        {
            x = a;
            y = b;
        }

        public Point2D(Point2D another)
        {
            x = another.x;
            y = another.y;
        }

        //calculate angle of line from origin to this point
        public double angle()
        {
            if (x == 0.0)
            {
                if (y > 0)
                    return 3.14 / 2.0;
                else
                    return 3.14 * 3.0 / 2.0;
            }
            double ang = Math.Atan(y / x);
            if (x > 0)
                return ang;

            return ang + 3.14;
        }

        //rotate this point about another point
        public void rotate(double angle, Point2D point)
        {
            x -= point.x;
            y -= point.y;

            double ox = x;
            double oy = y;

            x = Math.Cos(angle) * ox - Math.Sin(angle) * oy;
            y = Math.Sin(angle) * ox + Math.Cos(angle) * oy;

            x += point.x;
            y += point.y;
        }

        //what is the squared dist to another point
        public double distance_sq(Point2D point)
        {
            double dx = point.x - x;
            double dy = point.y - y;
            return dx * dx + dy * dy;
        }

        public double manhattenDistance(Point2D point)
        {
            double dx = Math.Abs(point.x - x);
            double dy = Math.Abs(point.y - y);
            return dx + dy;
        }
        //what is the distance to another point
        public double distance(Point2D point)
        {
            return Math.Sqrt(distance_sq(point));
        }
    }

    //line segment class, does some basic trig
    public class Line2D
    {
        public Point2D p1, p2;
        public Line2D() { }
        public Line2D(Point2D a, Point2D b)
        {
            p1 = a;
            p2 = b;
        }
        public Line2D(Line2D other)
        {
            p1 = other.p1;
            p2 = other.p2;
        }

        public void scale(double factor)
        {
            Point2D mid = midpoint();
            p1.x = mid.x + (p1.x - mid.x) * factor;
            p1.y = mid.y + (p1.y - mid.y) * factor;
            p2.x = mid.x + (p2.x - mid.x) * factor;
            p2.y = mid.y + (p2.y - mid.y) * factor;
        }

        //what is the midpoint of this line
        public Point2D midpoint()
        {
            double x = (p1.x + p2.x) / 2.0;
            double y = (p1.y + p2.y) / 2.0;
            return new Point2D(x, y);
        }

        //calculate the nearest intersection of this line
        //with a circle -- if the line is interpreted as a ray
        //going from its first endpoint to the second
        public double nearest_intersection(Circle2D C, out bool found)
        {
            double dx, dy;

            dx = p2.x - p1.x;
            dy = p2.y - p1.y;

            double px = p1.x - C.p.x;
            double py = p1.y - C.p.y;

            double a = dx * dx + dy * dy;
            double b = 2 * px * dx + 2 * py * dy;
            double c = px * px + py * py - C.radius * C.radius;

            double det = b * b - 4.0 * a * c;

            if (det < 0.0)
            {
                found = false;
                return -1.0;
            }

            double sqrt_det = Math.Sqrt(det);
            double t1 = (-b + sqrt_det) / (2 * a);
            double t2 = (-b - sqrt_det) / (2 * a);

            found = false;
            double t = 0.0;
            if (t2 < 0)
            {
                if (t1 > 0)
                {
                    found = true;
                    t = t1;
                }
            }
            else
            {
                found = true;
                t = t2;
            }
            if (!found)
                return -1.0;

            return t * Math.Sqrt(dx * dx + dy * dy);

        }

        //calculate the point of intersection between two line segments
        public Point2D intersection(Line2D L, out bool found)
        {
            Point2D pt = new Point2D(0.0, 0.0);
            Point2D A = p1;
            Point2D B = p2;
            Point2D C = L.p1;
            Point2D D = L.p2;

            double rTop = (A.y - C.y) * (D.x - C.x) - (A.x - C.x) * (D.y - C.y);
            double rBot = (B.x - A.x) * (D.y - C.y) - (B.y - A.y) * (D.x - C.x);

            double sTop = (A.y - C.y) * (B.x - A.x) - (A.x - C.x) * (B.y - A.y);
            double sBot = (B.x - A.x) * (D.y - C.y) - (B.y - A.y) * (D.x - C.x);

            if ((rBot == 0 || sBot == 0))
            {
                found = false;
                return pt;
            }
            double r = rTop / rBot;
            double s = sTop / sBot;
            if ((r > 0) && (r < 1) && (s > 0) && (s < 1))
            {
                pt.x = A.x + r * (B.x - A.x);
                pt.y = A.y + r * (B.y - A.y);
                found = true;
                return pt;
            }
            else
            {
                found = false;
                return pt;
            }
        }

        //what is the squared distance from this line to a point 
        public double distance_sq(Point2D n)
        {
            double utop = (n.x - p1.x) * (p2.x - p1.x) + (n.y - p1.y) * (p2.y - p1.y);
            double ubot = p1.distance_sq(p2);
            double u = utop / ubot;

            if (u < 0 || u > 1)
            {
                double d1 = p1.distance_sq(n);
                double d2 = p2.distance_sq(n);
                if (d1 < d2) return d1;
                return d2;
            }
            Point2D p = new Point2D(0.0, 0.0);
            p.x = p1.x + u * (p2.x - p1.x);
            p.y = p1.y + u * (p2.y - p1.y);
            return p.distance_sq(n);
        }

        //what is the distance from this line to a point
        public double distance(Point2D n)
        {
            return Math.Sqrt(distance_sq(n));
        }

        //what is the squared magnitude of this line segment
        public double length_sq()
        {
            return p1.distance_sq(p2);
        }
        //what is the length of this line segment
        public double length()
        {
            return p1.distance(p2);
        }
    }
}
