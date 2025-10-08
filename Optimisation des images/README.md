# Optimisation-des-images
Repository for a course project

## Utilisation
- Placez vos images d'entr�e (JPG/JPEG uniquement) dans le dossier images � la racine du d�p�t.
- Lancer le .bat ou se mettre dans "CSharpCourse\Optimisation des images" et faire .\run.bat

Sorties:
- Les images redimensionn�es sont �crites directement dans output/seq et output/par. 
- Chaque fichier de sortie est suffix� par la r�solution, ex: photo_1080p.jpg, photo_720p.jpg, photo_480p.jpg.

Le programme mesure les temps d'ex�cution des deux versions:
- Version sans optimisation: boucle s�quentielle qui traite les images une par une.
- Version optimis�e: Parallel.ForEachAsync (et t�ches) pour parall�liser le redimensionnement.

Les r�sultats de temps sont ajout�s automatiquement en bas de ce fichier dans une section "Benchmarks" apr�s chaque ex�cution.

## Benchmarks
- Sequential: 5256 ms for 6 images
- Parallel: 805 ms for 6 images
(Date: 2025-10-08 15:40:10)


## Benchmarks
- Sequential: 3585 ms for 6 images
- Parallel: 833 ms for 6 images
(Date: 2025-10-08 15:50:44)


## Benchmarks
- Sequential: 3228 ms for 6 images
- Parallel: 792 ms for 6 images
(Date: 2025-10-08 16:16:14)


## Benchmarks
- Sequential: 3559 ms for 6 images
- Parallel: 798 ms for 6 images
(Date: 2025-10-08 16:31:01)


## Benchmarks
- Sequential: 3267 ms for 6 images
- Parallel: 632 ms for 6 images
(Date: 2025-10-08 16:46:57)
